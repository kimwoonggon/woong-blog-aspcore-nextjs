using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class RealLoadTestControlPlane(
    IRealLoadTestRunRegistry runRegistry,
    RealLoadTestReportStore reportStore,
    IRealLoadTestRunner runner,
    IOptions<LoadTestingOptions> options,
    ILogger<RealLoadTestControlPlane> logger)
    : IRealLoadTestControlPlane
{
    public async Task<RealLoadTestStartResponse> StartAsync(RealLoadTestStartRequest request, CancellationToken cancellationToken)
    {
        var normalizedRequest = ValidateAndNormalize(request, options.Value);
        var nowUtc = DateTimeOffset.UtcNow;
        var runId = BuildRunId(nowUtc, normalizedRequest.Scenario);

        var run = new RealLoadTestRunEntry(
            runId,
            normalizedRequest.Runner,
            normalizedRequest.Scenario,
            normalizedRequest.Target,
            normalizedRequest.Rate,
            normalizedRequest.DurationSeconds,
            normalizedRequest.MaxVus,
            nowUtc);

        run.Status = RealLoadTestRunStates.Running;

        if (!runRegistry.TryAddRun(run, out var conflictReason))
        {
            throw new RealLoadTestConflictException($"Cannot start a new run: {conflictReason}");
        }

        await reportStore.InitializeRunAsync(run.ToStatusResponse(nowUtc), cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                await runner.RunAsync(run, CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled load test runner exception for run {RunId}", run.RunId);
            }
        }, CancellationToken.None);

        return new RealLoadTestStartResponse(
            run.RunId,
            run.Status,
            run.Runner,
            run.Scenario,
            run.StartedAtUtc);
    }

    public async Task<RealLoadTestStatusResponse> GetStatusAsync(string runId, CancellationToken cancellationToken)
    {
        var run = GetRunOrThrow(runId);
        RealLoadTestStatusResponse summary;
        lock (run.SyncRoot)
        {
            summary = run.ToStatusResponse(DateTimeOffset.UtcNow);
        }

        await reportStore.WriteSummaryAsync(summary, cancellationToken);
        return summary;
    }

    public async Task<RealLoadTestMetricsResponse> GetMetricsAsync(string runId, CancellationToken cancellationToken)
    {
        var run = GetRunOrThrow(runId);
        RealLoadTestStatusResponse summary;
        lock (run.SyncRoot)
        {
            summary = run.ToStatusResponse(DateTimeOffset.UtcNow);
        }

        var metrics = await reportStore.ReadMetricsAsync(run.RunId, cancellationToken);
        return new RealLoadTestMetricsResponse(
            run.RunId,
            summary.Status,
            summary.TotalRequests,
            summary.FailedRequests,
            summary.CurrentRps,
            summary.AverageRps,
            summary.P95Ms,
            summary.P99Ms,
            summary.MaxMs,
            summary.StatusCounts,
            metrics);
    }

    public async Task<RealLoadTestStopResponse> StopAsync(string runId, CancellationToken cancellationToken)
    {
        var run = GetRunOrThrow(runId);
        var shouldCancel = false;
        RealLoadTestStatusResponse summary;

        lock (run.SyncRoot)
        {
            if (string.Equals(run.Status, RealLoadTestRunStates.Stopped, StringComparison.OrdinalIgnoreCase))
            {
                summary = run.ToStatusResponse(DateTimeOffset.UtcNow);
            }
            else if (string.Equals(run.Status, RealLoadTestRunStates.Completed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(run.Status, RealLoadTestRunStates.Failed, StringComparison.OrdinalIgnoreCase))
            {
                summary = run.ToStatusResponse(DateTimeOffset.UtcNow);
            }
            else
            {
                run.Status = RealLoadTestRunStates.Stopped;
                run.EndedAtUtc = DateTimeOffset.UtcNow;
                summary = run.ToStatusResponse(DateTimeOffset.UtcNow);
                shouldCancel = true;
            }
        }

        if (shouldCancel)
        {
            run.CancellationTokenSource.Cancel();
        }

        await reportStore.WriteSummaryAsync(summary, cancellationToken);

        return new RealLoadTestStopResponse(run.RunId, summary.Status, summary.EndedAtUtc);
    }

    private RealLoadTestRunEntry GetRunOrThrow(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new RealLoadTestValidationException("runId is required.");
        }

        if (runRegistry.TryGetRun(runId.Trim(), out var run) && run is not null)
        {
            return run;
        }

        throw new RealLoadTestNotFoundException($"Run '{runId}' was not found.");
    }

    private static RealLoadTestStartRequest ValidateAndNormalize(
        RealLoadTestStartRequest request,
        LoadTestingOptions options)
    {
        if (request is null)
        {
            throw new RealLoadTestValidationException("Request payload is required.");
        }

        var scenario = RealLoadTestCatalog.Normalize(request.Scenario ?? string.Empty);
        var runner = RealLoadTestCatalog.Normalize(request.Runner ?? string.Empty);
        var target = RealLoadTestCatalog.Normalize(request.Target ?? string.Empty);

        if (!RealLoadTestCatalog.IsAllowedScenario(scenario))
        {
            throw new RealLoadTestValidationException($"Scenario '{request.Scenario}' is not allowed.");
        }

        if (!RealLoadTestCatalog.IsAllowedRunner(runner))
        {
            throw new RealLoadTestValidationException($"Runner '{request.Runner}' is not allowed.");
        }

        if (!RealLoadTestCatalog.IsAllowedTarget(target))
        {
            throw new RealLoadTestValidationException($"Target '{request.Target}' is not allowed.");
        }

        if (request.Rate < options.MinRate || request.Rate > options.MaxRate)
        {
            throw new RealLoadTestValidationException(
                $"Rate must be between {options.MinRate} and {options.MaxRate}.");
        }

        if (request.DurationSeconds < options.MinDurationSeconds || request.DurationSeconds > options.MaxDurationSeconds)
        {
            throw new RealLoadTestValidationException(
                $"DurationSeconds must be between {options.MinDurationSeconds} and {options.MaxDurationSeconds}.");
        }

        if (request.MaxVus < options.MinMaxVus || request.MaxVus > options.MaxMaxVus)
        {
            throw new RealLoadTestValidationException(
                $"MaxVus must be between {options.MinMaxVus} and {options.MaxMaxVus}.");
        }

        if (!options.UseFakeRunnerForTests && !string.Equals(runner, "fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new RealLoadTestValidationException("Only fake runner mode is currently enabled.");
        }

        return new RealLoadTestStartRequest(
            scenario,
            runner,
            target,
            request.Rate,
            request.DurationSeconds,
            request.MaxVus);
    }

    private static string BuildRunId(DateTimeOffset nowUtc, string scenario)
    {
        var slugScenario = new string(scenario
            .Where(character => char.IsLetterOrDigit(character) || character == '-')
            .ToArray());

        if (string.IsNullOrWhiteSpace(slugScenario))
        {
            slugScenario = "scenario";
        }

        return $"{nowUtc:yyyyMMdd-HHmmss}-{slugScenario}";
    }
}
