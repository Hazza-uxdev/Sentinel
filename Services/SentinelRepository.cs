using System.Text.Json;
using Sentinel.Models;

namespace Sentinel.Services;

public sealed class SentinelRepository
{
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public SentinelStore Load()
    {
        lock (_gate)
        {
            if (!File.Exists(AppPaths.StorePath))
                return new SentinelStore();

            try
            {
                return JsonSerializer.Deserialize<SentinelStore>(File.ReadAllText(AppPaths.StorePath)) ?? new SentinelStore();
            }
            catch
            {
                return new SentinelStore();
            }
        }
    }

    public void Save(SentinelStore store)
    {
        lock (_gate)
        {
            File.WriteAllText(AppPaths.StorePath, JsonSerializer.Serialize(store, Options));
        }
    }

    public void UpsertDownload(DownloadFinding finding)
    {
        var store = Load();
        ApplyFileSafeOverride(finding, store);

        store.Downloads.RemoveAll(x => SameFile(x.Path, finding.Path) && x.Sha256 == finding.Sha256);
        store.Downloads.Insert(0, finding);
        Save(store);
    }

    public DownloadFinding ApplyFileSafeOverride(DownloadFinding finding)
    {
        ApplyFileSafeOverride(finding, Load());
        return finding;
    }

    public void MarkSafe(DownloadFinding finding)
    {
        var store = Load();
        store.SafeOverrides.RemoveAll(x => SameFile(x.Path, finding.Path) && x.Sha256 == finding.Sha256);
        store.SafeOverrides.Add(new SafeOverride { Path = finding.Path, Sha256 = finding.Sha256 });

        foreach (var item in store.Downloads.Where(x => SameFile(x.Path, finding.Path) && x.Sha256 == finding.Sha256))
            ApplySafe(item);

        Save(store);
    }

    public int AddActivityAlerts(IEnumerable<LolbinAlert> alerts)
    {
        var store = Load();
        var visibleAlerts = alerts.Where(alert => !IsActivitySafe(alert, store)).ToList();
        foreach (var alert in visibleAlerts)
        {
            store.ActivityAlerts.RemoveAll(existing => SameActivity(existing, alert));
        }
        store.ActivityAlerts.InsertRange(0, visibleAlerts);
        store.ActivityAlerts = store.ActivityAlerts.Take(500).ToList();
        Save(store);
        return visibleAlerts.Count;
    }

    public void MarkActivitySafe(LolbinAlert alert)
    {
        var store = Load();
        store.ActivitySafeOverrides.RemoveAll(existing => SameActivityOverride(existing, alert));
        store.ActivitySafeOverrides.Add(new ActivitySafeOverride
        {
            ProcessName = alert.ProcessName,
            CommandLine = alert.CommandLine,
            MatchedRule = alert.MatchedRule,
            EncodedCommand = alert.EncodedCommand
        });
        store.ActivityAlerts.RemoveAll(existing => SameActivity(existing, alert));
        Save(store);
    }

    private static void ApplyFileSafeOverride(DownloadFinding finding, SentinelStore store)
    {
        if (store.SafeOverrides.Any(x => SameFile(x.Path, finding.Path) && x.Sha256 == finding.Sha256))
            ApplySafe(finding);
    }

    private static void ApplySafe(DownloadFinding finding)
    {
        finding.UserMarkedSafe = true;
        finding.Score = 0;
        finding.Severity = "Safe";
        finding.Explanation = "User marked this download as safe. Sentinel will no longer count this file toward threat scores.";
    }

    private static bool SameFile(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool IsActivitySafe(LolbinAlert alert, SentinelStore store) =>
        store.ActivitySafeOverrides.Any(existing => SameActivityOverride(existing, alert));

    private static bool SameActivity(LolbinAlert left, LolbinAlert right) =>
        string.Equals(left.ProcessName, right.ProcessName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.CommandLine, right.CommandLine, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.MatchedRule, right.MatchedRule, StringComparison.OrdinalIgnoreCase);

    private static bool SameActivityOverride(ActivitySafeOverride existing, LolbinAlert alert) =>
        string.Equals(existing.ProcessName, alert.ProcessName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(existing.CommandLine, alert.CommandLine, StringComparison.OrdinalIgnoreCase)
        && string.Equals(existing.MatchedRule, alert.MatchedRule, StringComparison.OrdinalIgnoreCase)
        && string.Equals(existing.EncodedCommand, alert.EncodedCommand, StringComparison.OrdinalIgnoreCase);
}
