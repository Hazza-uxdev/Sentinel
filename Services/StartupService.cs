using Microsoft.Win32;
using Sentinel.Models;

namespace Sentinel.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Sentinel";

    public void Apply(AppSettings settings)
    {
        if (!settings.OpenOnStartup)
        {
            Disable();
            return;
        }

        Enable(settings.OpenToTrayOnStartup);
    }

    private static void Enable(bool openToTray)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        var executable = Environment.ProcessPath ?? AppContext.BaseDirectory;
        var command = Quote(executable);
        if (openToTray)
            command += " --tray";
        key.SetValue(RunValueName, command);
    }

    private static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    private static string Quote(string value) => "\"" + value.Trim('"') + "\"";
}
