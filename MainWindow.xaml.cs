using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Sentinel.Models;
using Sentinel.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfListViewItem = System.Windows.Controls.ListViewItem;
using WpfListView = System.Windows.Controls.ListView;

namespace Sentinel;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly StartupService _startupService = new();
    private readonly SentinelRepository _repository = new();
    private readonly FileAnalyzer _fileAnalyzer = new();
    private readonly LolbinDetector _lolbinDetector = new();
    private readonly ReportExporter _reportExporter;
    private readonly DownloadMonitor _downloadMonitor;
    private readonly TrayNotificationService _tray;
    private readonly ObservableCollection<DownloadFinding> _downloads = [];
    private readonly ObservableCollection<LolbinAlert> _alerts = [];
    private readonly Dictionary<WpfButton, string> _busyButtons = [];
    private readonly Dictionary<WpfButton, object?> _buttonContent = [];
    private readonly DispatcherTimer _busyTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private static readonly TimeSpan MinimumBusyDuration = TimeSpan.FromMilliseconds(800);
    private AppSettings _settings;
    private DownloadFinding? _lastAnalyzedFile;
    private int _busyTick;
    private bool _isQuitting;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        _reportExporter = new ReportExporter(_repository);
        _downloadMonitor = new DownloadMonitor(_fileAnalyzer, _repository);
        _downloadMonitor.DownloadAnalyzed += DownloadMonitor_DownloadAnalyzed;
        _tray = new TrayNotificationService();
        _tray.OpenRequested += (_, _) => Dispatcher.Invoke(ShowFromTray);
        _tray.ScanDownloadsRequested += (_, _) => Dispatcher.Invoke(async () => await ScanDownloadsAsync());
        _tray.QuitRequested += (_, _) => Dispatcher.Invoke(Quit);
        _busyTimer.Tick += (_, _) => UpdateBusyButtons();

        DownloadsList.ItemsSource = _downloads;
        ActivityList.ItemsSource = _alerts;
        DownloadsPathText.Text = _settings.DownloadsPath;
        YaraPathText.Text = _settings.YaraRulesPath;
        AutoMonitorCheck.IsChecked = _settings.AutoMonitor;
        CloseToTrayCheck.IsChecked = _settings.CloseToTrayOnClose;
        OpenOnStartupCheck.IsChecked = _settings.OpenOnStartup;
        OpenToTrayStartupCheck.IsChecked = _settings.OpenToTrayOnStartup;
        ApplyStartupSetting(showErrors: false);

        UpdateGreeting();
        RefreshAll();
        ShowDashboard();
        if (_settings.AutoMonitor)
            StartMonitor();
        else
            SetActionStatus("Ready. Background monitoring is off.");
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        GreetingText.Text = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
    }

    private void RefreshAll()
    {
        var store = _repository.Load();
        _downloads.Clear();
        foreach (var item in store.Downloads)
            _downloads.Add(item);
        _alerts.Clear();
        foreach (var item in store.ActivityAlerts.Take(100))
            _alerts.Add(item);
        UpdateDashboard();
    }

    private void UpdateDashboard()
    {
        var store = _repository.Load();
        var suspicious = store.Downloads.Count(x => !x.UserMarkedSafe && x.Score >= 41);
        var high = store.Downloads.Where(x => !x.UserMarkedSafe).Select(x => x.Score).DefaultIfEmpty(0).Max();
        var latestDownload = store.Downloads.FirstOrDefault();
        var safeOverrides = store.SafeOverrides.Count + store.ActivitySafeOverrides.Count;
        DownloadsCountText.Text = store.Downloads.Count.ToString();
        SuspiciousCountText.Text = suspicious.ToString();
        ActivityCountText.Text = store.ActivityAlerts.Count.ToString();
        var status = high switch
        {
            >= 81 => ("High Risk", "#EF4444"),
            >= 61 => ("Warning", "#EF4444"),
            >= 21 => ("Monitor", "#F59E0B"),
            _ => ("Safe", "#22C55E")
        };
        StatusText.Text = status.Item1;
        StatusText.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(status.Item2)!;
        StatusExplanationText.Text = high == 0
            ? "No high-risk local download findings are currently counted."
            : $"Highest counted download score: {high}/100.";
        MonitorStateText.Text = _downloadMonitor.IsRunning
            ? $"Running in tray and watching {_settings.DownloadsPath}"
            : "Stopped. Use Start Monitor to resume background scanning.";
        RecentDownloadText.Text = latestDownload == null
            ? "No downloads analyzed yet."
            : $"Latest download: {latestDownload.FileName} - {latestDownload.Severity} ({latestDownload.Score}/100).";
        HighestRiskText.Text = $"Highest counted risk: {high}/100. Suspicious downloads: {suspicious}.";
        SafeOverrideText.Text = safeOverrides == 0
            ? "No user-safe overrides yet."
            : $"{safeOverrides} item(s) are ignored because you marked them safe.";
        ActivitySummaryText.Text = store.ActivityAlerts.Count == 0
            ? "No LOLBin alerts stored."
            : $"{store.ActivityAlerts.Count} activity alert(s) stored. Latest: {store.ActivityAlerts.First().ProcessName} - {store.ActivityAlerts.First().MatchedRule}.";
        LocalDataText.Text = $"Local store: {AppPaths.StorePath}";
        UpdateMonitorButton();
    }

    private void StartMonitor()
    {
        try
        {
            _downloadMonitor.Start(_settings.DownloadsPath);
            UpdateDashboard();
            UpdateMonitorButton();
            SetActionStatus($"Monitoring Downloads: {_settings.DownloadsPath}");
        }
        catch (Exception ex)
        {
            SetActionStatus("Unable to start monitoring: " + ex.Message, true);
            System.Windows.MessageBox.Show(ex.Message, "Sentinel monitor error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopMonitor()
    {
        _downloadMonitor.Stop();
        UpdateDashboard();
        UpdateMonitorButton();
        SetActionStatus("Download monitoring stopped.");
    }

    private async Task ScanDownloadsAsync()
    {
        try
        {
            SetActionStatus("Scanning existing downloads...");
            _downloads.Clear();
            var results = await _downloadMonitor.ScanExistingAsync(_settings.DownloadsPath);
            RefreshAll();
            SetActionStatus($"Scan complete. Analyzed {results.Count} file(s).");
        }
        catch (Exception ex)
        {
            SetActionStatus("Download scan failed: " + ex.Message, true);
            System.Windows.MessageBox.Show(ex.Message, "Sentinel scan error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DownloadMonitor_DownloadAnalyzed(object? sender, DownloadFinding finding)
    {
        Dispatcher.Invoke(() =>
        {
            RefreshAll();
            var stored = _repository.Load().Downloads.FirstOrDefault(x =>
                string.Equals(x.Path, finding.Path, StringComparison.OrdinalIgnoreCase)
                && x.Sha256 == finding.Sha256) ?? finding;
            if (!stored.UserMarkedSafe && stored.Score >= 41)
                _tray.NotifySuspiciousDownload(stored.FileName, stored.Severity, stored.Score, stored.Explanation);
        });
    }

    private void SetView(Grid view, System.Windows.Controls.Button active)
    {
        DashboardView.Visibility = DownloadsView.Visibility = FileAnalyzerView.Visibility =
            ActivityView.Visibility = ReportsView.Visibility = SettingsView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
        foreach (var button in new[] { BtnDashboard, BtnDownloads, BtnFileAnalyzer, BtnActivity, BtnReports, BtnSettings })
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
            button.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#777777")!;
        }
        active.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2D2D2D")!;
        active.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#E8E8E8")!;
    }

    private void ShowDashboard() => SetView(DashboardView, BtnDashboard);
    private void Dashboard_Click(object sender, RoutedEventArgs e) { ShowDashboard(); SetActionStatus("Dashboard opened."); }
    private void Downloads_Click(object sender, RoutedEventArgs e) { SetView(DownloadsView, BtnDownloads); SetActionStatus("Downloads opened."); }
    private void FileAnalyzer_Click(object sender, RoutedEventArgs e) { SetView(FileAnalyzerView, BtnFileAnalyzer); SetActionStatus("File Analyzer opened."); }
    private void Activity_Click(object sender, RoutedEventArgs e) { SetView(ActivityView, BtnActivity); SetActionStatus("Activity Monitor opened."); }
    private void Reports_Click(object sender, RoutedEventArgs e) { SetView(ReportsView, BtnReports); SetActionStatus("Reports opened."); }
    private void Settings_Click(object sender, RoutedEventArgs e) { SetView(SettingsView, BtnSettings); SetActionStatus("Settings opened."); }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (FindVisualParent<System.Windows.Controls.Button>(e.OriginalSource as DependencyObject) != null)
            return;
        if (e.ChangedButton != MouseButton.Left)
            return;
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => HideToTray();
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void CloseToTray_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.CloseToTrayOnClose)
            HideToTray();
        else
            Quit();
    }

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Window_StateChanged(object sender, EventArgs e)
    {
        RootBorder.BorderThickness = WindowState == WindowState.Maximized ? new Thickness(0) : new Thickness(1);
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
        if (!_downloadMonitor.IsRunning && _settings.AutoMonitor)
            StartMonitor();
    }

    public void StartHiddenInTray()
    {
        ShowInTaskbar = false;
        Hide();
        if (!_downloadMonitor.IsRunning && _settings.AutoMonitor)
            StartMonitor();
        SetActionStatus("Started in tray.");
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        SetActionStatus("Restored from tray.");
    }

    private void Quit_Click(object sender, RoutedEventArgs e) => Quit();

    private void Quit()
    {
        _isQuitting = true;
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isQuitting && _settings.CloseToTrayOnClose)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        _downloadMonitor.Dispose();
        _tray.Dispose();
        base.OnClosing(e);
    }

    private async void StartMonitor_Click(object sender, RoutedEventArgs e)
    {
        var label = _downloadMonitor.IsRunning ? "Stopping" : "Starting";
        if (sender is WpfButton button)
        {
            await RunButtonBusyAsync(button, label, () =>
            {
                ToggleMonitor();
                return Task.CompletedTask;
            });
            UpdateMonitorButton();
        }
        else
        {
            ToggleMonitor();
        }
    }

    private void ToggleMonitor()
    {
        if (_downloadMonitor.IsRunning)
            StopMonitor();
        else
            StartMonitor();
    }

    private async void ScanDownloads_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton button)
            await RunButtonBusyAsync(button, "Scanning", ScanDownloadsAsync);
        else
            await ScanDownloadsAsync();
    }

    private void RefreshDownloads_Click(object sender, RoutedEventArgs e)
    {
        _downloads.Clear();
        RefreshAll();
        SetActionStatus("Download history refreshed.");
    }

    private void MarkSafe_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadsList.SelectedItem is not DownloadFinding finding)
        {
            System.Windows.MessageBox.Show("Select a download first.", "Sentinel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _repository.MarkSafe(finding);
        RefreshAll();
        SetActionStatus($"Marked safe: {finding.FileName}");
    }

    private void MarkActivitySafe_Click(object sender, RoutedEventArgs e)
    {
        if (ActivityList.SelectedItem is not LolbinAlert alert)
        {
            System.Windows.MessageBox.Show("Select an activity alert first.", "Sentinel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _repository.MarkActivitySafe(alert);
        RefreshAll();
        SetActionStatus($"Marked activity safe: {alert.ProcessName} / {alert.MatchedRule}");
    }

    private void MarkAnalyzedFileSafe_Click(object sender, RoutedEventArgs e)
    {
        if (_lastAnalyzedFile == null)
        {
            System.Windows.MessageBox.Show("Analyze a file first.", "Sentinel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _repository.MarkSafe(_lastAnalyzedFile);
        _lastAnalyzedFile = _repository.ApplyFileSafeOverride(_lastAnalyzedFile);
        AnalyzerOutput.Text = _fileAnalyzer.Format(_lastAnalyzedFile);
        RefreshAll();
        SetActionStatus($"Marked analyzed file safe: {_lastAnalyzedFile.FileName}");
    }

    private void DownloadsList_RightClick(object sender, MouseButtonEventArgs e) => SelectRightClickedRow(DownloadsList, e);

    private void ActivityList_RightClick(object sender, MouseButtonEventArgs e) => SelectRightClickedRow(ActivityList, e);

    private static void SelectRightClickedRow(WpfListView list, MouseButtonEventArgs e)
    {
        var row = FindVisualParent<WpfListViewItem>(e.OriginalSource as DependencyObject);
        if (row == null)
            return;
        row.IsSelected = true;
        row.Focus();
        list.SelectedItem = row.DataContext;
    }

    private async void ScanActivity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton button)
            await RunButtonBusyAsync(button, "Scanning", ScanActivityAsync);
        else
            await ScanActivityAsync();
    }

    private async Task ScanActivityAsync()
    {
        try
        {
            SetActionStatus("Scanning running processes...");
            var alerts = await Task.Run(() => _lolbinDetector.ScanProcesses());
            var visible = _repository.AddActivityAlerts(alerts);
            RefreshAll();
            SetActionStatus($"Activity scan complete. Found {visible} visible alert(s).");
        }
        catch (Exception ex)
        {
            SetActionStatus("Activity scan failed: " + ex.Message, true);
            System.Windows.MessageBox.Show(ex.Message, "Sentinel activity scan error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyEncoded_Click(object sender, RoutedEventArgs e)
    {
        if (ActivityList.SelectedItem is not LolbinAlert alert)
        {
            System.Windows.MessageBox.Show("Select an activity alert first.", "Sentinel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(alert.EncodedCommand))
        {
            System.Windows.MessageBox.Show("The selected alert does not contain an encoded PowerShell command.", "Sentinel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        System.Windows.Clipboard.SetText(alert.EncodedCommand);
        SetActionStatus("Encoded PowerShell command copied to clipboard.");
    }

    private async void AnalyzeFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true)
        {
            SetActionStatus("File analysis cancelled.");
            return;
        }
        if (sender is WpfButton button)
            await RunButtonBusyAsync(button, "Analyzing", () => AnalyzeSelectedFileAsync(dialog.FileName));
        else
            await AnalyzeSelectedFileAsync(dialog.FileName);
    }

    private async Task AnalyzeSelectedFileAsync(string path)
    {
        try
        {
            var finding = await Task.Run(() => _fileAnalyzer.Analyze(path));
            _lastAnalyzedFile = _repository.ApplyFileSafeOverride(finding);
            AnalyzerOutput.Text = _fileAnalyzer.Format(_lastAnalyzedFile);
            SetActionStatus($"Analyzed file: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            AnalyzerOutput.Text = "Analysis failed:\n" + ex.Message;
            SetActionStatus("File analysis failed: " + ex.Message, true);
        }
    }

    private void OpenYaraFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_settings.YaraRulesPath);
        Process.Start(new ProcessStartInfo(_settings.YaraRulesPath) { UseShellExecute = true });
        SetActionStatus("Opened YARA rules folder.");
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton button)
            await RunButtonBusyAsync(button, "Exporting", async () =>
            {
                var path = await Task.Run(_reportExporter.ExportJson);
                ReportOutput.Text = path;
                SetActionStatus("Exported JSON report: " + path);
            });
    }

    private async void ExportHtml_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton button)
            await RunButtonBusyAsync(button, "Exporting", async () =>
            {
                var path = await Task.Run(_reportExporter.ExportHtml);
                ReportOutput.Text = path;
                SetActionStatus("Exported HTML report: " + path);
            });
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton button)
            await RunButtonBusyAsync(button, "Saving", SaveSettingsAsync);
        else
            await SaveSettingsAsync();
    }

    private Task SaveSettingsAsync()
    {
        _settings.DownloadsPath = DownloadsPathText.Text;
        _settings.YaraRulesPath = YaraPathText.Text;
        _settings.AutoMonitor = AutoMonitorCheck.IsChecked == true;
        _settings.CloseToTrayOnClose = CloseToTrayCheck.IsChecked == true;
        _settings.OpenOnStartup = OpenOnStartupCheck.IsChecked == true;
        _settings.OpenToTrayOnStartup = OpenToTrayStartupCheck.IsChecked == true;
        _settingsService.Save(_settings);
        ApplyStartupSetting(showErrors: true);
        if (_settings.AutoMonitor)
            StartMonitor();
        else
            _downloadMonitor.Stop();
        UpdateDashboard();
        UpdateMonitorButton();
        SetActionStatus("Settings saved.");
        return Task.CompletedTask;
    }

    private void ApplyStartupSetting(bool showErrors)
    {
        try
        {
            _startupService.Apply(_settings);
        }
        catch (Exception ex)
        {
            SetActionStatus("Unable to update Windows startup setting: " + ex.Message, true);
            if (showErrors)
                System.Windows.MessageBox.Show(ex.Message, "Sentinel startup setting error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task RunButtonBusyAsync(WpfButton button, string label, Func<Task> action)
    {
        var started = DateTime.UtcNow;
        BeginButtonBusy(button, label);
        try
        {
            await action();
        }
        finally
        {
            var remaining = MinimumBusyDuration - (DateTime.UtcNow - started);
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);
            EndButtonBusy(button);
        }
    }

    private void BeginButtonBusy(WpfButton button, string label)
    {
        if (!_buttonContent.ContainsKey(button))
            _buttonContent[button] = button.Content;
        _busyButtons[button] = label;
        button.IsEnabled = false;
        _busyTick = 0;
        UpdateBusyButtons();
        if (!_busyTimer.IsEnabled)
            _busyTimer.Start();
    }

    private void EndButtonBusy(WpfButton button)
    {
        if (_buttonContent.Remove(button, out var content))
            button.Content = content;
        _busyButtons.Remove(button);
        button.IsEnabled = true;
        if (_busyButtons.Count == 0)
            _busyTimer.Stop();
    }

    private void UpdateBusyButtons()
    {
        _busyTick++;
        var dots = new string('.', _busyTick % 4);
        if (dots.Length == 0)
            dots = ".";
        foreach (var item in _busyButtons)
            item.Key.Content = $"{item.Value} {dots}";
    }

    private void UpdateMonitorButton()
    {
        if (MonitorToggleButton == null || _busyButtons.ContainsKey(MonitorToggleButton))
            return;
        MonitorToggleButton.Content = _downloadMonitor.IsRunning ? "Stop Monitor" : "Start Monitor";
    }

    private void SetActionStatus(string message, bool isError = false)
    {
        if (ActionStatusText == null)
            return;
        ActionStatusText.Text = $"{DateTime.Now:t}  {message}";
        ActionStatusText.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter()
            .ConvertFromString(isError ? "#EF4444" : "#777777")!;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T target)
                return target;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
