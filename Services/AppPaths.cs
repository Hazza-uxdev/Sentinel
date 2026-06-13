namespace Sentinel.Services;

public static class AppPaths
{
    public static string LocalDataDirectory
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sentinel");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string SettingsPath => Path.Combine(LocalDataDirectory, "settings-csharp.json");
    public static string StorePath => Path.Combine(LocalDataDirectory, "sentinel-store.json");
    public static string ReportsDirectory
    {
        get
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Reports");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string YaraRulesDirectory
    {
        get
        {
            var path = Path.Combine(AppContext.BaseDirectory, "YaraRules");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
