using System.Text.Json.Serialization;

namespace Sentinel.Models;

public sealed class LolbinAlert
{
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
    public string CommandLine { get; set; } = "";
    public string MatchedRule { get; set; } = "";
    public string Severity { get; set; } = "Medium";
    public string Reason { get; set; } = "";
    public string EncodedCommand { get; set; } = "";

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
