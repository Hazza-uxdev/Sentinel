using System.Text.Json.Serialization;

namespace Sentinel.Models;

public sealed class DownloadFinding
{
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Md5 { get; set; } = "";
    public string Sha1 { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Category { get; set; } = "Other";
    public int Score { get; set; }
    public string Severity { get; set; } = "Safe";
    public string Explanation { get; set; } = "";
    public bool UserMarkedSafe { get; set; }

    [JsonIgnore]
    public string DisplayScore => UserMarkedSafe ? "" : Score.ToString();

    [JsonIgnore]
    public System.Windows.Media.Brush SeverityBrush => Severity switch
    {
        "Critical" => System.Windows.Media.Brushes.IndianRed,
        "High" => System.Windows.Media.Brushes.IndianRed,
        "Medium" => System.Windows.Media.Brushes.Goldenrod,
        "Low" => System.Windows.Media.Brushes.YellowGreen,
        _ => System.Windows.Media.Brushes.LightGreen
    };
}
