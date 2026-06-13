using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Sentinel.Models;

namespace Sentinel.Services;

public sealed class FileAnalyzer
{
    private static readonly HashSet<string> Executables = new(StringComparer.OrdinalIgnoreCase) { ".exe", ".dll", ".scr", ".sys" };
    private static readonly HashSet<string> Scripts = new(StringComparer.OrdinalIgnoreCase) { ".js", ".vbs", ".ps1", ".cmd", ".bat" };
    private static readonly HashSet<string> Archives = new(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z", ".iso", ".img", ".tar", ".gz" };
    private static readonly HashSet<string> Shortcuts = new(StringComparer.OrdinalIgnoreCase) { ".lnk", ".url" };
    private static readonly HashSet<string> DisguiseExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".gif", ".txt"
    };

    public async Task<bool> WaitUntilStableAsync(string path, CancellationToken token)
    {
        long previous = -1;
        var stableTicks = 0;
        for (var i = 0; i < 60; i++)
        {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(path))
                return false;

            var size = new FileInfo(path).Length;
            if (size == previous)
            {
                stableTicks++;
                if (stableTicks >= 2)
                    return true;
            }
            else
            {
                previous = size;
                stableTicks = 0;
            }

            await Task.Delay(500, token);
        }
        return false;
    }

    public DownloadFinding Analyze(string path)
    {
        var file = new FileInfo(path);
        var hashes = HashFile(path);
        var ext = file.Extension;
        var score = 0;
        var reasons = new List<string>();

        if (Executables.Contains(ext))
        {
            score += 15;
            reasons.Add("the file is an executable program downloaded to disk");
        }

        if (Scripts.Contains(ext))
        {
            score += 20;
            reasons.Add("the file is a script that can run commands on this computer");
        }

        if (HasDoubleExtension(file.Name))
        {
            score += 30;
            reasons.Add("the filename appears to disguise its real file type");
        }

        if (file.Name.Length > 80)
        {
            score += 10;
            reasons.Add("the filename is unusually long");
        }

        if (LooksRandom(Path.GetFileNameWithoutExtension(file.Name)))
        {
            score += 10;
            reasons.Add("the filename looks randomly generated");
        }

        if (Archives.Contains(ext) && Regex.IsMatch(file.Name, "(exe|setup|installer|crack|patch)", RegexOptions.IgnoreCase))
        {
            score += 10;
            reasons.Add("the archive name suggests it may contain a program");
        }

        score = Math.Clamp(score, 0, 100);
        var severity = SeverityForScore(score);
        var explanation = reasons.Count == 0
            ? "No suspicious filename or file type indicators were found."
            : "Risk increased because " + string.Join("; ", reasons) + $". Final assessment: {severity} ({score}/100).";

        return new DownloadFinding
        {
            FileName = file.Name,
            Path = file.FullName,
            Size = file.Length,
            CreatedAt = file.CreationTime,
            ModifiedAt = file.LastWriteTime,
            Md5 = hashes.Md5,
            Sha1 = hashes.Sha1,
            Sha256 = hashes.Sha256,
            Category = CategoryFor(ext),
            Score = score,
            Severity = severity,
            Explanation = explanation
        };
    }

    public string AnalyzeText(string path)
    {
        var result = Analyze(path);
        return Format(result);
    }

    public string Format(DownloadFinding result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"File: {result.FileName}");
        builder.AppendLine($"Path: {result.Path}");
        builder.AppendLine($"Size: {result.Size:N0} bytes");
        builder.AppendLine();
        builder.AppendLine($"MD5:    {result.Md5}");
        builder.AppendLine($"SHA1:   {result.Sha1}");
        builder.AppendLine($"SHA256: {result.Sha256}");
        builder.AppendLine();
        builder.AppendLine($"Category: {result.Category}");
        builder.AppendLine($"Risk: {result.Severity} ({result.Score}/100)");
        builder.AppendLine(result.Explanation);
        return builder.ToString();
    }

    private static (string Md5, string Sha1, string Sha256) HashFile(string path)
    {
        using var md5 = MD5.Create();
        using var sha1 = SHA1.Create();
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(path);
        var md5Bytes = md5.ComputeHash(stream);
        stream.Position = 0;
        var sha1Bytes = sha1.ComputeHash(stream);
        stream.Position = 0;
        var sha256Bytes = sha256.ComputeHash(stream);
        return (Hex(md5Bytes), Hex(sha1Bytes), Hex(sha256Bytes));
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static string CategoryFor(string ext)
    {
        if (Executables.Contains(ext)) return "Executable";
        if (Scripts.Contains(ext)) return "Script";
        if (Archives.Contains(ext)) return "Archive";
        if (Shortcuts.Contains(ext)) return "Shortcut";
        return "Other";
    }

    private static string SeverityForScore(int score) => score switch
    {
        <= 20 => "Safe",
        <= 40 => "Low",
        <= 60 => "Medium",
        <= 80 => "High",
        _ => "Critical"
    };

    private static bool HasDoubleExtension(string name)
    {
        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return false;
        var previous = "." + parts[^2];
        var current = "." + parts[^1];
        return DisguiseExts.Contains(previous) && (Executables.Contains(current) || Scripts.Contains(current));
    }

    private static bool LooksRandom(string name)
    {
        var cleaned = Regex.Replace(name, "[^a-zA-Z0-9]", "");
        if (cleaned.Length < 10)
            return false;

        var digits = cleaned.Count(char.IsDigit);
        var letters = cleaned.Count(char.IsLetter);
        var vowels = cleaned.Count(c => "aeiouAEIOU".Contains(c));
        var uniqueRatio = cleaned.ToLowerInvariant().Distinct().Count() / (double)cleaned.Length;
        var digitRatio = digits / (double)cleaned.Length;
        var vowelRatio = letters == 0 ? 0 : vowels / (double)letters;
        return uniqueRatio > 0.65 && digitRatio > 0.25 || cleaned.Length >= 12 && vowelRatio < 0.18;
    }
}
