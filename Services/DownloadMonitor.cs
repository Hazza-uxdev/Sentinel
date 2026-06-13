using Sentinel.Models;
using System.IO;

namespace Sentinel.Services;

public sealed class DownloadMonitor : IDisposable
{
    private readonly FileAnalyzer _analyzer;
    private readonly SentinelRepository _repository;
    private FileSystemWatcher? _watcher;
    private readonly HashSet<string> _recent = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<DownloadFinding>? DownloadAnalyzed;

    public bool IsRunning => _watcher?.EnableRaisingEvents == true;

    public DownloadMonitor(FileAnalyzer analyzer, SentinelRepository repository)
    {
        _analyzer = analyzer;
        _repository = repository;
    }

    public void Start(string downloadsPath)
    {
        Directory.CreateDirectory(downloadsPath);
        Stop();
        _watcher = new FileSystemWatcher(downloadsPath)
        {
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite
        };
        _watcher.Created += (_, e) => QueueAnalyze(e.FullPath);
        _watcher.Renamed += (_, e) => QueueAnalyze(e.FullPath);
    }

    public void Stop()
    {
        if (_watcher == null)
            return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
    }

    public Task<List<DownloadFinding>> ScanExistingAsync(string downloadsPath) =>
        Task.Run(() =>
        {
            Directory.CreateDirectory(downloadsPath);
            var results = new List<DownloadFinding>();
            foreach (var file in Directory.EnumerateFiles(downloadsPath))
            {
                if (ShouldSkip(file))
                    continue;
                try
                {
                    var result = _analyzer.Analyze(file);
                    _repository.UpsertDownload(result);
                    results.Add(result);
                }
                catch (IOException)
                {
                    Thread.Sleep(200);
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            return results;
        });

    private void QueueAnalyze(string path)
    {
        if (ShouldSkip(path))
            return;

        lock (_recent)
        {
            if (!_recent.Add(path))
                return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
                if (!await _analyzer.WaitUntilStableAsync(path, cts.Token))
                    return;
                var result = _analyzer.Analyze(path);
                _repository.UpsertDownload(result);
                DownloadAnalyzed?.Invoke(this, result);
            }
            catch
            {
            }
            finally
            {
                await Task.Delay(1500);
                lock (_recent)
                    _recent.Remove(path);
            }
        });
    }

    private static bool ShouldSkip(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".crdownload", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".part", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
               || Directory.Exists(path);
    }

    public void Dispose() => Stop();
}
