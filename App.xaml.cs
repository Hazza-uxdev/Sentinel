namespace Sentinel;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        if (e.Args.Any(arg => string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase)))
            window.StartHiddenInTray();
        else
            window.Show();
    }
}
