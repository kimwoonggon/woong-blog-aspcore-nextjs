using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class RealLoadTestReportStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _reportsRoot;

    public RealLoadTestReportStore(IHostEnvironment environment, IOptions<LoadTestingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        var settings = options.Value;
        var configuredRoot = string.IsNullOrWhiteSpace(settings.ReportsRelativeRoot)
            ? Path.Combine("reports", "loadtest")
            : settings.ReportsRelativeRoot;

        _reportsRoot = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot);
    }

    public async Task InitializeRunAsync(RealLoadTestStatusResponse summary, CancellationToken cancellationToken)
    {
        var runDirectory = GetRunDirectory(summary.RunId);
        Directory.CreateDirectory(runDirectory);
        await WriteSummaryAsync(summary, cancellationToken);

        var metricsPath = GetMetricsPath(summary.RunId);
        if (!File.Exists(metricsPath))
        {
            await File.WriteAllTextAsync(metricsPath, string.Empty, Encoding.UTF8, cancellationToken);
        }
    }

    public async Task WriteSummaryAsync(RealLoadTestStatusResponse summary, CancellationToken cancellationToken)
    {
        var gate = GetFileLock(summary.RunId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var summaryPath = GetSummaryPath(summary.RunId);
            Directory.CreateDirectory(Path.GetDirectoryName(summaryPath)!);
            var content = JsonSerializer.Serialize(summary, JsonOptions);
            await File.WriteAllTextAsync(summaryPath, content, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AppendMetricAsync(string runId, RealLoadTestMetricPoint metric, CancellationToken cancellationToken)
    {
        var gate = GetFileLock(runId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var metricsPath = GetMetricsPath(runId);
            Directory.CreateDirectory(Path.GetDirectoryName(metricsPath)!);
            var line = JsonSerializer.Serialize(metric, JsonOptions);
            await File.AppendAllTextAsync(metricsPath, $"{line}{Environment.NewLine}", Encoding.UTF8, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<RealLoadTestMetricPoint>> ReadMetricsAsync(string runId, CancellationToken cancellationToken)
    {
        var metricsPath = GetMetricsPath(runId);
        if (!File.Exists(metricsPath))
        {
            return Array.Empty<RealLoadTestMetricPoint>();
        }

        var gate = GetFileLock(runId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var lines = await File.ReadAllLinesAsync(metricsPath, cancellationToken);
            if (lines.Length == 0)
            {
                return Array.Empty<RealLoadTestMetricPoint>();
            }

            var metrics = new List<RealLoadTestMetricPoint>(lines.Length);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var metric = JsonSerializer.Deserialize<RealLoadTestMetricPoint>(line, JsonOptions);
                if (metric is not null)
                {
                    metrics.Add(metric);
                }
            }

            return metrics;
        }
        finally
        {
            gate.Release();
        }
    }

    private string GetRunDirectory(string runId)
    {
        return Path.Combine(_reportsRoot, runId);
    }

    private string GetSummaryPath(string runId)
    {
        return Path.Combine(GetRunDirectory(runId), "summary.json");
    }

    private string GetMetricsPath(string runId)
    {
        return Path.Combine(GetRunDirectory(runId), "metrics.ndjson");
    }

    private SemaphoreSlim GetFileLock(string runId)
    {
        return _fileLocks.GetOrAdd(runId, static _ => new SemaphoreSlim(1, 1));
    }
}
