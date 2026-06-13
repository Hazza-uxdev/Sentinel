using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using Sentinel.Models;

namespace Sentinel.Services;

public sealed class LolbinDetector
{
    private sealed record Rule(string Name, string[] Patterns, string Severity, string Reason);

    private readonly Dictionary<string, List<Rule>> _rules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["powershell.exe"] =
        [
            new("Encoded PowerShell command", ["-enc", "-encodedcommand"], "High",
                "PowerShell is running an encoded command. Malware commonly uses encoded commands to hide activity."),
            new("PowerShell web download", ["downloadstring", "invoke-webrequest", "iwr", "http://", "https://"], "High",
                "PowerShell appears to be downloading content from the internet, which is commonly abused by malware."),
            new("PowerShell inline execution", ["iex", "invoke-expression"], "High",
                "PowerShell is executing text as code. Attackers use this to run commands without saving a file.")
        ],
        ["pwsh.exe"] =
        [
            new("Encoded PowerShell command", ["-enc", "-encodedcommand"], "High",
                "PowerShell is running an encoded command. Malware commonly uses encoded commands to hide activity.")
        ],
        ["certutil.exe"] =
        [
            new("Certutil URL cache download", ["-urlcache", "http://", "https://"], "High",
                "Certutil is being used with a web address. Attackers commonly abuse this Windows tool to download files.")
        ],
        ["mshta.exe"] =
        [
            new("MSHTA remote or script execution", ["http://", "https://", "javascript:"], "High",
                "MSHTA is running script content or opening a web address. This is a common malware technique.")
        ],
        ["regsvr32.exe"] =
        [
            new("Regsvr32 scriptlet execution", ["scrobj.dll", "/i:http", "/i:https"], "High",
                "Regsvr32 appears to be loading a remote scriptlet. This can bypass normal script controls.")
        ],
        ["rundll32.exe"] =
        [
            new("Rundll32 script or URL handler abuse", ["javascript:", "url.dll"], "Medium",
                "Rundll32 is being used in a way often seen in suspicious script or URL launches.")
        ],
        ["wscript.exe"] =
        [
            new("Windows Script Host script execution", ["http://", "https://", ".js", ".vbs"], "Medium",
                "Windows Script Host is running script content. Unexpected scripts can change system settings or download files.")
        ],
        ["cscript.exe"] =
        [
            new("Console Script Host script execution", ["http://", "https://", ".js", ".vbs"], "Medium",
                "Console Script Host is running script content. Unexpected scripts can change system settings or download files.")
        ],
        ["wmic.exe"] =
        [
            new("WMIC process creation", ["process", "call", "create"], "Medium",
                "WMIC can start processes remotely or locally. Unexpected process creation should be checked.")
        ],
        ["msiexec.exe"] =
        [
            new("MSI installer remote source", ["http://", "https://", "/i"], "Medium",
                "Windows Installer appears to be installing from a web address. Confirm the source is trusted.")
        ],
        ["bitsadmin.exe"] =
        [
            new("BITS transfer", ["http://", "https://", "/transfer"], "Medium",
                "BITSAdmin can download files in the background. Unexpected use may deserve investigation.")
        ]
    };

    public List<LolbinAlert> ScanProcesses()
    {
        var commandLines = GetCommandLines();
        var alerts = new List<LolbinAlert>();
        foreach (var process in Process.GetProcesses())
        {
            var name = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? process.ProcessName
                : process.ProcessName + ".exe";
            if (!_rules.TryGetValue(name, out var rules))
                continue;

            commandLines.TryGetValue(process.Id, out var commandLine);
            commandLine ??= name;
            alerts.AddRange(MatchProcess(name, process.Id, commandLine, rules));
        }
        return alerts;
    }

    private static Dictionary<int, string> GetCommandLines()
    {
        var results = new Dictionary<int, string>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process");
            foreach (var obj in searcher.Get())
            {
                var pid = Convert.ToInt32(obj["ProcessId"]);
                var commandLine = obj["CommandLine"]?.ToString();
                if (!string.IsNullOrWhiteSpace(commandLine))
                    results[pid] = commandLine;
            }
        }
        catch
        {
        }
        return results;
    }

    private static IEnumerable<LolbinAlert> MatchProcess(string name, int pid, string commandLine, List<Rule> rules)
    {
        var lower = commandLine.ToLowerInvariant();
        foreach (var rule in rules)
        {
            if (!rule.Patterns.Any(pattern => lower.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                continue;

            var encoded = name.Contains("powershell", StringComparison.OrdinalIgnoreCase)
                ? ExtractEncodedCommand(commandLine)
                : "";
            var reason = rule.Reason;
            if (!string.IsNullOrWhiteSpace(encoded) && rule.Name.Contains("encoded", StringComparison.OrdinalIgnoreCase))
                reason += " Encoded command value: " + encoded;

            yield return new LolbinAlert
            {
                ProcessName = name,
                Pid = pid,
                CommandLine = commandLine,
                MatchedRule = rule.Name,
                Severity = rule.Severity,
                Reason = reason,
                EncodedCommand = encoded
            };
        }
    }

    private static string ExtractEncodedCommand(string commandLine)
    {
        var match = Regex.Match(
            commandLine,
            "(?i)(?:^|\\s)-(?:enc|encodedcommand)(?:\\s+|:)(?:\"([A-Za-z0-9+/=]+)\"|'([A-Za-z0-9+/=]+)'|([A-Za-z0-9+/=]+))");
        if (!match.Success)
            return "";
        return match.Groups.Cast<Group>().Skip(1).FirstOrDefault(group => group.Success)?.Value ?? "";
    }
}
