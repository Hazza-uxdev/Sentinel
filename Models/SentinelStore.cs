namespace Sentinel.Models;

public sealed class SentinelStore
{
    public List<DownloadFinding> Downloads { get; set; } = [];
    public List<LolbinAlert> ActivityAlerts { get; set; } = [];
    public List<SafeOverride> SafeOverrides { get; set; } = [];
    public List<ActivitySafeOverride> ActivitySafeOverrides { get; set; } = [];
}

public sealed class SafeOverride
{
    public string Path { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public DateTime MarkedAt { get; set; } = DateTime.Now;
}

public sealed class ActivitySafeOverride
{
    public string ProcessName { get; set; } = "";
    public string CommandLine { get; set; } = "";
    public string MatchedRule { get; set; } = "";
    public string EncodedCommand { get; set; } = "";
    public DateTime MarkedAt { get; set; } = DateTime.Now;
}
