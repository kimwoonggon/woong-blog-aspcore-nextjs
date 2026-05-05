using Microsoft.Extensions.Logging;

namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class FakeRealLoadTestRunner(RealLoadTestReportStore reportStore, ILogger<FakeRealLoadTestRunner> logger)
    : IRealLoadTestRunner
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    public async Task RunAsync(RealLoadTestRunEntry run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            run.CancellationTokenSource.Token);
        var linkedCancellationToken = linkedCancellationTokenSource.Token;
        var maxTicks = Math.Max(4, Math.Min(run.DurationSeconds * 4, 7200));

        try
        {
            lock (run.SyncRoot)
            {
                run.Status = RealLoadTestRunStates.Running;
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), linkedCancellationToken);

            for (var tick = 1; tick <= maxTicks; tick++)
            {
                linkedCancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TickInterval, linkedCancellationToken);

                RealLoadTestMetricPoint metric;
                lock (run.SyncRoot)
                {
                    ApplyTick(run, tick);
                    metric = CaptureMetric(run);
                }

                await reportStore.AppendMetricAsync(run.RunId, metric, linkedCancellationToken);
                await reportStore.WriteSummaryAsync(CaptureSummary(run), linkedCancellationToken);
            }

            lock (run.SyncRoot)
            {
                if (!string.Equals(run.Status, RealLoadTestRunStates.Stopped, StringComparison.OrdinalIgnoreCase))
                {
                    run.Status = RealLoadTestRunStates.Completed;
                    run.EndedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), linkedCancellationToken);
        }
        catch (OperationCanceledException)
        {
            lock (run.SyncRoot)
            {
                if (!string.Equals(run.Status, RealLoadTestRunStates.Stopped, StringComparison.OrdinalIgnoreCase))
                {
                    run.Status = RealLoadTestRunStates.Stopped;
                    run.EndedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Fake real load test runner failed for run {RunId}", run.RunId);

            lock (run.SyncRoot)
            {
                run.Status = RealLoadTestRunStates.Failed;
                run.EndedAtUtc = DateTimeOffset.UtcNow;
                run.StatusCounts["5xx"] += 1;
                run.FailedRequests += 1;
            }

            await reportStore.WriteSummaryAsync(CaptureSummary(run), CancellationToken.None);
        }
        finally
        {
            linkedCancellationTokenSource.Dispose();
        }
    }

    private static void ApplyTick(RealLoadTestRunEntry run, int tick)
    {
        var requestsForTick = Math.Max(1, run.Rate / 4);
        var simulatedFailures = tick % 11 == 0 ? 1 : 0;
        var successfulRequests = Math.Max(0, requestsForTick - simulatedFailures);
        var target = run.Targets.Count == 0
            ? new RealLoadTestTargetSpec("public-api", "Public API", "/api/public/works?page=1&pageSize=12", "work")
            : run.Targets[(tick - 1) % run.Targets.Count];
        var p50Ms = 55 + (tick % 7);
        var p95Ms = 120 + (tick % 11) * 3;
        var p99Ms = p95Ms + 35 + (tick % 5);
        var maxMs = p99Ms + 40 + (tick % 9);

        run.TotalRequests += requestsForTick;
        run.FailedRequests += simulatedFailures;
        run.StatusCounts["2xx"] += successfulRequests;
        run.StatusCounts["5xx"] += simulatedFailures;

        var elapsedSeconds = Math.Max(0.001, (DateTimeOffset.UtcNow - run.StartedAtUtc).TotalSeconds);
        run.CurrentRps = Math.Max(0, successfulRequests / TickInterval.TotalSeconds);
        run.AverageRps = Math.Round(run.TotalRequests / elapsedSeconds, 2);
        run.P50Ms = p50Ms;
        run.P95Ms = p95Ms;
        run.P99Ms = p99Ms;
        run.MaxMs = maxMs;
        run.LatencyBreakdown = new RealLoadTestLatencyBreakdown(
            Math.Max(1, p50Ms - 35),
            p50Ms,
            p95Ms,
            p99Ms,
            maxMs,
            Math.Max(1, p95Ms - 18),
            p95Ms + 6,
            Math.Max(1, p95Ms - 8),
            "fake.nginx_upstream");
        ApplyTargetMetrics(run, target, requestsForTick, successfulRequests, simulatedFailures, p95Ms);
    }

    private static void ApplyTargetMetrics(
        RealLoadTestRunEntry run,
        RealLoadTestTargetSpec target,
        int requestCount,
        int successCount,
        int failureCount,
        double p95Ms)
    {
        run.TargetMetrics.TryGetValue(target.Id, out var existing);
        var statusCounts = existing is null
            ? RealLoadTestRunEntry.CreateStatusCounts()
            : new Dictionary<string, long>(existing.StatusCounts, StringComparer.OrdinalIgnoreCase);

        statusCounts["2xx"] += successCount;
        statusCounts["5xx"] += failureCount;

        var nextRequestCount = (existing?.RequestCount ?? 0) + requestCount;
        var nextFailureCount = (existing?.FailureCount ?? 0) + failureCount;
        var nextSuccessCount = (existing?.SuccessCount ?? 0) + successCount;
        var nextP95 = existing is null ? p95Ms : Math.Max(existing.P95Ms, p95Ms);

        run.TargetMetrics[target.Id] = new RealLoadTestTargetMetrics(
            target.Id,
            target.Label,
            target.Path,
            target.Group,
            nextRequestCount,
            nextSuccessCount,
            nextFailureCount,
            nextP95,
            statusCounts);
    }

    private static RealLoadTestStatusResponse CaptureSummary(RealLoadTestRunEntry run)
    {
        lock (run.SyncRoot)
        {
            return run.ToStatusResponse(DateTimeOffset.UtcNow);
        }
    }

    private static RealLoadTestMetricPoint CaptureMetric(RealLoadTestRunEntry run)
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
            snapshot.TargetMetrics);
    }
}
