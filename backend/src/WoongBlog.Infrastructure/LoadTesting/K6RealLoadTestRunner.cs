using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class K6RealLoadTestRunner(
    RealLoadTestReportStore reportStore,
    ILoadTestDiagnosticsSampler diagnosticsSampler,
    IOptions<LoadTestingOptions> options,
    ILogger<K6RealLoadTestRunner> logger)
    : IRealLoadTestRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RunAsync(RealLoadTestRunEntry run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            run.CancellationTokenSource.Token);
        var linkedCancellationToken = linkedCancellationTokenSource.Token;
        var runDirectory = Path.Combine(Path.GetTempPath(), "woongblog-k6", run.RunId);
        var scriptPath = Path.Combine(runDirectory, "run.js");
        var summaryPath = Path.Combine(runDirectory, "summary.json");
        Process? process = null;

        try
        {
            Directory.CreateDirectory(runDirectory);
            await File.WriteAllTextAsync(scriptPath, BuildScript(), Encoding.UTF8, linkedCancellationToken);

            lock (run.SyncRoot)
            {
                run.Status = RealLoadTestRunStates.Running;
            }
            await reportStore.WriteSummaryAsync(CaptureSummary(run), linkedCancellationToken);

            process = StartK6Process(run, scriptPath, summaryPath);
            await process.WaitForExitAsync(linkedCancellationToken);

            if (File.Exists(summaryPath))
            {
                await ApplyK6SummaryAsync(run, summaryPath, linkedCancellationToken);
            }

            lock (run.SyncRoot)
            {
                if (process.ExitCode == 0
                    && !string.Equals(run.Status, RealLoadTestRunStates.Stopped, StringComparison.OrdinalIgnoreCase))
                {
                    run.Status = RealLoadTestRunStates.Completed;
                    run.EndedAtUtc = DateTimeOffset.UtcNow;
                }
                else if (!string.Equals(run.Status, RealLoadTestRunStates.Stopped, StringComparison.OrdinalIgnoreCase))
                {
                    run.Status = RealLoadTestRunStates.Failed;
                    run.EndedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
            }

            lock (run.SyncRoot)
            {
                run.Status = RealLoadTestRunStates.Stopped;
                run.EndedAtUtc = DateTimeOffset.UtcNow;
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "k6 real load test runner failed for run {RunId}", run.RunId);

            lock (run.SyncRoot)
            {
                run.Status = RealLoadTestRunStates.Failed;
                run.EndedAtUtc = DateTimeOffset.UtcNow;
                run.FailedRequests = Math.Max(1, run.FailedRequests);
                run.StatusCounts["5xx"] = Math.Max(1, run.StatusCounts["5xx"]);
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), CancellationToken.None);
        }
        finally
        {
            process?.Dispose();
        }
    }

    private Process StartK6Process(RealLoadTestRunEntry run, string scriptPath, string summaryPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.Value.K6ExecutablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--summary-export");
        startInfo.ArgumentList.Add(summaryPath);
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.Environment["BASE_URL"] = options.Value.BaseUrl;
        startInfo.Environment["SCENARIO"] = run.Scenario;
        startInfo.Environment["RATE"] = run.Rate.ToString();
        startInfo.Environment["PEAK_RATE"] = run.PeakRate.ToString();
        startInfo.Environment["DURATION_SECONDS"] = run.DurationSeconds.ToString();
        startInfo.Environment["MAX_VUS"] = run.MaxVus.ToString();
        startInfo.Environment["START_VUS"] = run.StartVus.ToString();
        startInfo.Environment["TARGETS_JSON"] = JsonSerializer.Serialize(
            run.Targets.Select(target => new
            {
                id = target.Id,
                label = target.Label,
                path = target.Path,
                group = target.Group,
                metricId = SanitizeMetricId(target.Id)
            }),
            JsonOptions);

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start k6 process.");

        _ = Task.Run(async () =>
        {
            var stdout = await process.StandardOutput.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                logger.LogInformation("k6 stdout for run {RunId}: {Output}", run.RunId, stdout.Trim());
            }
        });
        _ = Task.Run(async () =>
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                logger.LogWarning("k6 stderr for run {RunId}: {Output}", run.RunId, stderr.Trim());
            }
        });

        return process;
    }

    private async Task ApplyK6SummaryAsync(RealLoadTestRunEntry run, string summaryPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(summaryPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var metrics = document.RootElement.GetProperty("metrics");
        var totalRequests = ReadMetricValue(metrics, "http_reqs", "count");
        var currentRps = ReadMetricValue(metrics, "http_reqs", "rate");
        var failedRequests = ReadMetricValue(metrics, "http_req_failed", "passes");
        var duration = ReadTrend(metrics, "http_req_duration");
        var statusCounts = ReadStatusCounts(metrics);
        var targetMetrics = ReadTargetMetrics(run, metrics);
        var appElapsedP95 = targetMetrics
            .Select(target => ReadTrendMetric(metrics, $"target_{SanitizeMetricId(target.TargetId)}_app_elapsed", "p(95)"))
            .DefaultIfEmpty(0)
            .Max();
        var nginxRequestP95 = MaxNullable(run.Targets.Select(target => ReadOptionalTrendMetric(metrics, $"target_{SanitizeMetricId(target.Id)}_nginx_request", "p(95)")));
        var nginxUpstreamHeaderP95 = MaxNullable(run.Targets.Select(target => ReadOptionalTrendMetric(metrics, $"target_{SanitizeMetricId(target.Id)}_nginx_upstream", "p(95)")));
        var nginxUpstreamFallbackP95 = MaxNullable(run.Targets.Select(target => ReadOptionalTrendMetric(metrics, $"target_{SanitizeMetricId(target.Id)}_nginx_upstream_waiting_fallback", "p(95)")));
        var nginxUpstreamP95 = nginxUpstreamHeaderP95 ?? nginxUpstreamFallbackP95;
        var nginxUpstreamP95Source = nginxUpstreamHeaderP95.HasValue
            ? "nginx.upstream_response_time.header"
            : nginxUpstreamFallbackP95.HasValue
                ? "runner.http_waiting_fallback"
                : null;

        lock (run.SyncRoot)
        {
            run.TotalRequests = (long)Math.Round(totalRequests);
            run.FailedRequests = (long)Math.Round(failedRequests);
            run.CurrentRps = Round(currentRps);
            run.AverageRps = Round(currentRps);
            run.P50Ms = duration.P50Ms;
            run.P95Ms = duration.P95Ms;
            run.P99Ms = duration.P99Ms;
            run.MaxMs = duration.MaxMs;
            foreach (var statusCount in statusCounts)
            {
                run.StatusCounts[statusCount.Key] = statusCount.Value;
            }

            run.LatencyBreakdown = new RealLoadTestLatencyBreakdown(
                duration.MinMs,
                duration.P50Ms,
                duration.P95Ms,
                duration.P99Ms,
                duration.MaxMs,
                appElapsedP95,
                nginxRequestP95,
                nginxUpstreamP95,
                nginxUpstreamP95Source);

            run.TargetMetrics.Clear();
            foreach (var targetMetric in targetMetrics)
            {
                run.TargetMetrics[targetMetric.TargetId] = targetMetric;
            }
        }

        var diagnostics = await diagnosticsSampler.CaptureAsync(cancellationToken);
        await reportStore.AppendMetricAsync(run.RunId, CaptureMetric(run, diagnostics), cancellationToken);
        await reportStore.WriteSummaryAsync(CaptureSummary(run), cancellationToken);
    }

    private static IReadOnlyList<RealLoadTestTargetMetrics> ReadTargetMetrics(RealLoadTestRunEntry run, JsonElement metrics)
    {
        return run.Targets.Select(target =>
        {
            var metricId = SanitizeMetricId(target.Id);
            var requestCount = (long)Math.Round(ReadMetricValue(metrics, $"target_{metricId}_requests", "count"));
            var failureCount = (long)Math.Round(ReadMetricValue(metrics, $"target_{metricId}_failed", "count"));
            var statusCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["2xx"] = (long)Math.Round(ReadMetricValue(metrics, $"target_{metricId}_status_2xx", "count")),
                ["3xx"] = (long)Math.Round(ReadMetricValue(metrics, $"target_{metricId}_status_3xx", "count")),
                ["4xx"] = (long)Math.Round(ReadMetricValue(metrics, $"target_{metricId}_status_4xx", "count")),
                ["5xx"] = (long)Math.Round(ReadMetricValue(metrics, $"target_{metricId}_status_5xx", "count"))
            };

            return new RealLoadTestTargetMetrics(
                target.Id,
                target.Label,
                target.Path,
                target.Group,
                requestCount,
                Math.Max(0, requestCount - failureCount),
                failureCount,
                ReadTrendMetric(metrics, $"target_{metricId}_duration", "p(95)"),
                statusCounts);
        }).ToArray();
    }

    private static IReadOnlyDictionary<string, long> ReadStatusCounts(JsonElement metrics)
    {
        return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["2xx"] = (long)Math.Round(ReadMetricValue(metrics, "status_2xx", "count")),
            ["3xx"] = (long)Math.Round(ReadMetricValue(metrics, "status_3xx", "count")),
            ["4xx"] = (long)Math.Round(ReadMetricValue(metrics, "status_4xx", "count")),
            ["5xx"] = (long)Math.Round(ReadMetricValue(metrics, "status_5xx", "count")),
            ["429"] = (long)Math.Round(ReadMetricValue(metrics, "status_429", "count")),
            ["503"] = (long)Math.Round(ReadMetricValue(metrics, "status_503", "count")),
            ["timeout"] = 0
        };
    }

    private static (double MinMs, double P50Ms, double P95Ms, double P99Ms, double MaxMs) ReadTrend(JsonElement metrics, string name)
    {
        return (
            ReadTrendMetric(metrics, name, "min"),
            ReadTrendMetric(metrics, name, "med"),
            ReadTrendMetric(metrics, name, "p(95)"),
            Round(ReadOptionalTrendMetric(metrics, name, "p(99)")
                ?? ReadOptionalTrendMetric(metrics, name, "p(95)")
                ?? 0),
            ReadTrendMetric(metrics, name, "max"));
    }

    private static double ReadTrendMetric(JsonElement metrics, string name, string valueName)
    {
        return Round(ReadOptionalTrendMetric(metrics, name, valueName) ?? 0);
    }

    private static double? ReadOptionalTrendMetric(JsonElement metrics, string name, string valueName)
    {
        if (!metrics.TryGetProperty(name, out var metric))
        {
            return null;
        }

        var valueSource = metric.TryGetProperty("values", out var values)
            ? values
            : metric;

        if (!valueSource.TryGetProperty(valueName, out var value)
            || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.GetDouble();
    }

    private static double ReadMetricValue(JsonElement metrics, string name, string valueName)
    {
        if (!metrics.TryGetProperty(name, out var metric))
        {
            return 0;
        }

        var valueSource = metric.TryGetProperty("values", out var values)
            ? values
            : metric;

        if (!valueSource.TryGetProperty(valueName, out var value)
            || value.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        return value.GetDouble();
    }

    private static double? MaxNullable(IEnumerable<double?> values)
    {
        var nonNull = values.Where(static value => value.HasValue).Select(static value => value!.Value).ToArray();
        return nonNull.Length == 0 ? null : Round(nonNull.Max());
    }

    private static RealLoadTestStatusResponse CaptureSummary(RealLoadTestRunEntry run)
    {
        lock (run.SyncRoot)
        {
            return run.ToStatusResponse(DateTimeOffset.UtcNow);
        }
    }

    private static RealLoadTestMetricPoint CaptureMetric(
        RealLoadTestRunEntry run,
        LoadTestDiagnosticsSnapshot diagnostics)
    {
        var snapshot = run.ToStatusResponse(DateTimeOffset.UtcNow);
        return new RealLoadTestMetricPoint(
            DateTimeOffset.UtcNow,
            snapshot.ElapsedSeconds,
            snapshot.TotalRequests,
            snapshot.FailedRequests,
            snapshot.CurrentRps,
            snapshot.AverageRps,
            snapshot.P95Ms,
            snapshot.P99Ms,
            snapshot.MaxMs,
            snapshot.StatusCounts,
            run.LatencyBreakdown,
            snapshot.TargetMetrics,
            diagnostics);
    }

    private static string SanitizeMetricId(string id)
    {
        var characters = id.Select(static character =>
            char.IsLetterOrDigit(character) ? character : '_').ToArray();
        return new string(characters);
    }

    private static double Round(double value)
    {
        return Math.Round(value, 1);
    }

    internal static string BuildScriptForTests()
    {
        return BuildScript();
    }

    private static string BuildScript()
    {
        return """
            import http from 'k6/http';
            import { check, sleep } from 'k6';
            import { Counter, Trend } from 'k6/metrics';

            const baseUrl = (__ENV.BASE_URL || 'http://127.0.0.1:3000').replace(/\/$/, '');
            const scenario = __ENV.SCENARIO || 'public-api-rps';
            const rate = Number.parseInt(__ENV.RATE || '10', 10);
            const peakRate = Number.parseInt(__ENV.PEAK_RATE || `${rate * 2}`, 10);
            const durationSeconds = Number.parseInt(__ENV.DURATION_SECONDS || '30', 10);
            const maxVus = Number.parseInt(__ENV.MAX_VUS || '10', 10);
            const startVus = Number.parseInt(__ENV.START_VUS || `${Math.max(1, Math.floor(maxVus * 0.1))}`, 10);
            const targets = JSON.parse(__ENV.TARGETS_JSON || '[]');
            const summaryTrendStats = ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max', 'count'];
            const thresholds = {
              http_req_failed: ['rate<0.01'],
              http_req_duration: ['p(95)<800', 'p(99)<1500'],
            };

            const status2xx = new Counter('status_2xx');
            const status3xx = new Counter('status_3xx');
            const status4xx = new Counter('status_4xx');
            const status5xx = new Counter('status_5xx');
            const status429 = new Counter('status_429');
            const status503 = new Counter('status_503');
            const targetMetrics = {};

            for (const target of targets) {
              targetMetrics[target.id] = {
                requests: new Counter(`target_${target.metricId}_requests`),
                failed: new Counter(`target_${target.metricId}_failed`),
                status2xx: new Counter(`target_${target.metricId}_status_2xx`),
                status3xx: new Counter(`target_${target.metricId}_status_3xx`),
                status4xx: new Counter(`target_${target.metricId}_status_4xx`),
                status5xx: new Counter(`target_${target.metricId}_status_5xx`),
                duration: new Trend(`target_${target.metricId}_duration`),
                appElapsed: new Trend(`target_${target.metricId}_app_elapsed`),
                nginxRequest: new Trend(`target_${target.metricId}_nginx_request`),
                nginxUpstream: new Trend(`target_${target.metricId}_nginx_upstream`),
                nginxUpstreamWaitingFallback: new Trend(`target_${target.metricId}_nginx_upstream_waiting_fallback`),
              };
            }

            export const options = buildOptions();

            function buildOptions() {
              if (scenario === 'public-api-soak') {
                return {
                  summaryTrendStats,
                  thresholds,
                  scenarios: { public_api_soak: { executor: 'constant-vus', vus: maxVus, duration: `${durationSeconds}s` } },
                };
              }

              if (scenario === 'public-api-spike') {
                return {
                  summaryTrendStats,
                  thresholds,
                  scenarios: {
                    public_api_spike: {
                      executor: 'ramping-arrival-rate',
                      startRate: rate,
                      timeUnit: '1s',
                      preAllocatedVUs: Math.min(200, maxVus),
                      maxVUs: maxVus,
                      stages: [
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.25))}s`, target: peakRate },
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.25))}s`, target: peakRate },
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.25))}s`, target: rate },
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.25))}s`, target: rate },
                      ],
                    },
                  },
                };
              }

              if (scenario === 'public-api-stress') {
                return {
                  summaryTrendStats,
                  thresholds,
                  scenarios: {
                    public_api_stress: {
                      executor: 'ramping-vus',
                      startVUs: startVus,
                      stages: [
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.33))}s`, target: Math.max(1, Math.floor(maxVus * 0.5)) },
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.33))}s`, target: maxVus },
                        { duration: `${Math.max(5, Math.floor(durationSeconds * 0.33))}s`, target: startVus },
                      ],
                    },
                  },
                };
              }

              return {
                summaryTrendStats,
                thresholds,
                scenarios: {
                  public_api_rps: {
                    executor: 'constant-arrival-rate',
                    rate,
                    timeUnit: '1s',
                    duration: `${durationSeconds}s`,
                    preAllocatedVUs: Math.min(200, maxVus),
                    maxVUs: maxVus,
                  },
                },
              };
            }

            export default function publicApiTarget() {
              const target = targets.length ? targets[(__ITER + __VU - 1) % targets.length] : { id: 'default', path: '/api/public/works?page=1&pageSize=12' };
              const url = buildUrl(target.path);
              const response = http.get(url, { tags: { target_id: target.id, target_group: target.group || 'unknown' } });
              const metrics = targetMetrics[target.id];

              if (metrics) {
                metrics.requests.add(1);
                metrics.duration.add(response.timings.duration);
              }

              recordStatus(response.status, metrics);
              recordOptionalHeaderMetric(response, 'X-App-Elapsed-Ms', metrics?.appElapsed, 1);
              recordOptionalHeaderMetric(response, 'X-Nginx-Request-Time', metrics?.nginxRequest, 1000);
              const recordedUpstreamHeader = recordOptionalHeaderMetric(response, ['X-Nginx-Upstream-Response-Time', 'X-Nginx-Upstream-Time'], metrics?.nginxUpstream, 1000);
              if (!recordedUpstreamHeader) {
                metrics?.nginxUpstreamWaitingFallback.add(response.timings.waiting);
              }
              check(response, { 'status is < 500': (result) => result.status < 500 });

              if (scenario === 'public-api-soak') {
                sleep(1);
              }
            }

            function buildUrl(path) {
              if (/^https?:\/\//i.test(path)) {
                return appendIdentity(path);
              }

              return appendIdentity(`${baseUrl}${path}`);
            }

            function appendIdentity(url) {
              const separator = url.includes('?') ? '&' : '?';
              return `${url}${separator}__k6Vu=${__VU}&__k6Iter=${__ITER}`;
            }

            function recordStatus(status, metrics) {
              if (status >= 200 && status < 300) {
                status2xx.add(1);
                metrics?.status2xx.add(1);
                return;
              }
              if (status >= 300 && status < 400) {
                status3xx.add(1);
                metrics?.status3xx.add(1);
                return;
              }
              if (status >= 400 && status < 500) {
                status4xx.add(1);
                metrics?.status4xx.add(1);
                if (status === 429) status429.add(1);
                metrics?.failed.add(1);
                return;
              }

              status5xx.add(1);
              if (status === 503) status503.add(1);
              metrics?.status5xx.add(1);
              metrics?.failed.add(1);
            }

            function recordOptionalHeaderMetric(response, headerNames, trend, multiplier) {
              if (!trend) return false;
              const names = Array.isArray(headerNames) ? headerNames : [headerNames];
              for (const headerName of names) {
                const raw = response.headers[headerName] || response.headers[headerName.toLowerCase()];
                const value = parseTimingHeader(raw, multiplier);
                if (value !== null) {
                  trend.add(value);
                  return true;
                }
              }
              return false;
            }

            function parseTimingHeader(raw, multiplier) {
              if (!raw) return null;
              const values = String(raw)
                .split(/[,:]/)
                .map((part) => Number.parseFloat(part.trim()))
                .filter((value) => Number.isFinite(value));

              if (!values.length) return null;

              return values.reduce((sum, value) => sum + value, 0) * multiplier;
            }
            """;
    }
}
