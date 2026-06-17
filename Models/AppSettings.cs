namespace Sentinel.Models;

public sealed class AppSettings
{
    public string DownloadsPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads");

    public string YaraRulesPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "YaraRules");

    public bool AutoMonitor { get; set; } = true;

    public bool CloseToTrayOnClose { get; set; } = true;

    public bool OpenOnStartup { get; set; } = true;

    public bool OpenToTrayOnStartup { get; set; } = true;
}
