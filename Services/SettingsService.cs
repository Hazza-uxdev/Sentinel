using System.Text.Json;
using Sentinel.Models;

namespace Sentinel.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Load()
    {
        if (!File.Exists(AppPaths.SettingsPath))
        {
            var defaults = new AppSettings { YaraRulesPath = AppPaths.YaraRulesDirectory };
            Save(defaults);
            return defaults;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.SettingsPath)) ?? new AppSettings();
            if (string.IsNullOrWhiteSpace(settings.YaraRulesPath))
                settings.YaraRulesPath = AppPaths.YaraRulesDirectory;
            Directory.CreateDirectory(settings.DownloadsPath);
            Directory.CreateDirectory(settings.YaraRulesPath);
            return settings;
        }
        catch
        {
            return new AppSettings { YaraRulesPath = AppPaths.YaraRulesDirectory };
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(settings.DownloadsPath);
        Directory.CreateDirectory(settings.YaraRulesPath);
        File.WriteAllText(AppPaths.SettingsPath, JsonSerializer.Serialize(settings, Options));
    }
}
