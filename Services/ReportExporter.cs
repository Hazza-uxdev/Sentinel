using System.Net;
using System.Text;
using System.Text.Json;

namespace Sentinel.Services;

public sealed class ReportExporter
{
    private readonly SentinelRepository _repository;

    public ReportExporter(SentinelRepository repository)
    {
        _repository = repository;
    }

    public string ExportJson()
    {
        var store = _repository.Load();
        var path = Path.Combine(AppPaths.ReportsDirectory, $"sentinel_report_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    public string ExportHtml()
    {
        var store = _repository.Load();
        var path = Path.Combine(AppPaths.ReportsDirectory, $"sentinel_report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Sentinel Report</title>");
        builder.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;background:#161616;color:#e8e8e8;margin:32px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #333;padding:8px;text-align:left}th{color:#777}</style>");
        builder.AppendLine("</head><body><h1>Sentinel Report</h1><h2>Downloads</h2><table><tr><th>File</th><th>Severity</th><th>Score</th><th>Explanation</th></tr>");
        foreach (var item in store.Downloads)
        {
            builder.AppendLine($"<tr><td>{WebUtility.HtmlEncode(item.FileName)}</td><td>{item.Severity}</td><td>{item.DisplayScore}</td><td>{WebUtility.HtmlEncode(item.Explanation)}</td></tr>");
        }
        builder.AppendLine("</table><h2>Activity Alerts</h2><table><tr><th>Process</th><th>PID</th><th>Severity</th><th>Rule</th><th>Reason</th></tr>");
        foreach (var item in store.ActivityAlerts)
        {
            builder.AppendLine($"<tr><td>{WebUtility.HtmlEncode(item.ProcessName)}</td><td>{item.Pid}</td><td>{item.Severity}</td><td>{WebUtility.HtmlEncode(item.MatchedRule)}</td><td>{WebUtility.HtmlEncode(item.Reason)}</td></tr>");
        }
        builder.AppendLine("</table></body></html>");
        File.WriteAllText(path, builder.ToString());
        return path;
    }
}
