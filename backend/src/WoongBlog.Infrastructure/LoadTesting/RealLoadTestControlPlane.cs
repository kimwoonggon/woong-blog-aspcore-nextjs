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
        RealLoadTestStartRequest normalizedRequest;
        try
        {
            normalizedRequest = ValidateAndNormalize(request, options.Value);
        }
        catch (RealLoadTestValidationException exception)
        {
            logger.LogWarning(
                exception,
                "Load test start validation failed. runId={RunId}, runner={Runner}, scenario={Scenario}, target={Target}, rate={Rate}, durationSeconds={DurationSeconds}, maxVUS={MaxVus}, validationReason={ValidationReason}",
                string.Empty,
                request?.Runner ?? string.Empty,
                request?.Scenario ?? string.Empty,
                request?.Target ?? string.Empty,
                request?.Rate ?? 0,
                request?.DurationSeconds ?? 0,
                request?.MaxVus ?? 0,
                exception.Message);
            throw;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var runId = BuildRunId(nowUtc, normalizedRequest.Scenario);

        logger.LogInformation(
            "Starting load test request with runId={RunId}, runner={Runner}, scenario={Scenario}, target={Target}, rate={Rate}, durationSeconds={DurationSeconds}, maxVUS={MaxVus}",
            runId,
            normalizedRequest.Runner,
            normalizedRequest.Scenario,
            normalizedRequest.Target,
            normalizedRequest.Rate,
            normalizedRequest.DurationSeconds,
            normalizedRequest.MaxVus);

        var run = new RealLoadTestRunEntry(
            runId,
            normalizedRequest.Runner,
            normalizedRequest.Scenario,
            normalizedRequest.Target,
            normalizedRequest.Rate,
            normalizedRequest.PeakRate ?? normalizedRequest.Rate,
            normalizedRequest.DurationSeconds,
            normalizedRequest.MaxVus,
            normalizedRequest.StartVus ?? 1,
            normalizedRequest.Targets,
            nowUtc);

        run.Status = RealLoadTestRunStates.Running;

        if (!runRegistry.TryAddRun(run, out var conflictReason))
        {
            logger.LogWarning(
                "Load test start blocked by conflict. runId={RunId}, runner={Runner}, scenario={Scenario}, target={Target}, rate={Rate}, durationSeconds={DurationSeconds}, maxVUS={MaxVus}, conflict={ConflictReason}",
                runId,
                run.Runner,
                run.Scenario,
                run.Target,
                run.Rate,
                run.DurationSeconds,
                run.MaxVus,
                conflictReason);

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
                logger.LogError(
                    exception,
                    "Unhandled load test runner exception for run {RunId}, runner={Runner}, scenario={Scenario}, target={Target}, rate={Rate}, durationSeconds={DurationSeconds}, maxVUS={MaxVus}",
                    run.RunId,
                    run.Runner,
                    run.Scenario,
                    run.Target,
                    run.Rate,
                    run.DurationSeconds,
                    run.MaxVus);
            }
        }, CancellationToken.None);

        return new RealLoadTestStartResponse(
            run.RunId,
            run.Status,
            run.Runner,
            run.Scenario,
            run.Targets,
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
            run.LatencyBreakdown,
            summary.TargetMetrics,
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

        logger.LogInformation(
            "Stop requested for run {RunId}, runner={Runner}, scenario={Scenario}, target={Target}, rate={Rate}, durationSeconds={DurationSeconds}, maxVUS={MaxVus}, shouldCancel={ShouldCancel}",
            run.RunId,
            run.Runner,
            run.Scenario,
            run.Target,
            run.Rate,
            run.DurationSeconds,
            run.MaxVus,
            shouldCancel);

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

        var peakRate = NormalizePeakRate(request, scenario, options);
        var startVus = NormalizeStartVus(request, scenario, options);
        var effectiveRunner = options.UseFakeRunnerForTests || !options.RealRunnerEnabled
            ? "fake"
            : runner;
        var targets = NormalizeTargets(request.Targets, target);

        return new RealLoadTestStartRequest(
            scenario,
            effectiveRunner,
            target,
            request.Rate,
            peakRate,
            request.DurationSeconds,
            request.MaxVus,
            startVus,
            targets);
    }

    private static int NormalizePeakRate(
        RealLoadTestStartRequest request,
        string scenario,
        LoadTestingOptions options)
    {
        var defaultPeakRate = scenario == "public-api-spike"
            ? (int)Math.Min(options.MaxRate, (long)request.Rate * 2)
            : request.Rate;
        var peakRate = request.PeakRate ?? defaultPeakRate;

        if (peakRate < options.MinRate || peakRate > options.MaxRate)
        {
            throw new RealLoadTestValidationException(
                $"PeakRate must be between {options.MinRate} and {options.MaxRate}.");
        }

        if (scenario == "public-api-spike" && peakRate < request.Rate)
        {
            throw new RealLoadTestValidationException("PeakRate must be greater than or equal to Rate for public-api-spike.");
        }

        return peakRate;
    }

    private static int NormalizeStartVus(
        RealLoadTestStartRequest request,
        string scenario,
        LoadTestingOptions options)
    {
        var defaultStartVus = scenario == "public-api-stress"
            ? Math.Max(options.MinMaxVus, request.MaxVus / 10)
            : options.MinMaxVus;
        var startVus = request.StartVus ?? defaultStartVus;

        if (startVus < options.MinMaxVus || startVus > request.MaxVus)
        {
            throw new RealLoadTestValidationException(
                $"StartVus must be between {options.MinMaxVus} and MaxVus.");
        }

        return startVus;
    }

    private static IReadOnlyList<RealLoadTestTargetSpec> NormalizeTargets(
        IReadOnlyList<RealLoadTestTargetSpec>? requestedTargets,
        string targetPreset)
    {
        var candidates = requestedTargets is { Count: > 0 }
            ? requestedTargets
            : DefaultTargetsForPreset(targetPreset);

        var normalized = candidates
            .Select(NormalizeTarget)
            .Where(static target => target is not null)
            .Cast<RealLoadTestTargetSpec>()
            .Where(target => TargetMatchesPreset(target, targetPreset))
            .GroupBy(static target => target.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToArray();

        if (normalized.Length == 0)
        {
            normalized = DefaultTargetsForPreset(targetPreset).ToArray();
        }

        return normalized;
    }

    private static RealLoadTestTargetSpec? NormalizeTarget(RealLoadTestTargetSpec? target)
    {
        if (target is null)
        {
            return null;
        }

        var id = NormalizeToken(target.Id);
        var label = string.IsNullOrWhiteSpace(target.Label) ? id : target.Label.Trim();
        var path = target.Path?.Trim() ?? string.Empty;
        var group = RealLoadTestCatalog.Normalize(target.Group ?? string.Empty);

        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(path)
            || (group != "work" && group != "study")
            || !IsAllowedTargetPath(path))
        {
            return null;
        }

        return new RealLoadTestTargetSpec(id, label, path, group);
    }

    private static bool TargetMatchesPreset(RealLoadTestTargetSpec target, string targetPreset)
    {
        return targetPreset switch
        {
            "public-works-only" => target.Group == "work",
            "public-blogs-only" => target.Group == "study",
            _ => true
        };
    }

    private static IReadOnlyList<RealLoadTestTargetSpec> DefaultTargetsForPreset(string targetPreset)
    {
        var targets = new[]
        {
            new RealLoadTestTargetSpec("works-list", "Work list", "/api/public/works?page=1&pageSize=12", "work"),
            new RealLoadTestTargetSpec("study-list", "Study list", "/api/public/blogs?page=1&pageSize=12", "study")
        };

        return targets.Where(target => TargetMatchesPreset(target, targetPreset)).ToArray();
    }

    private static string NormalizeToken(string value)
    {
        var characters = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-')
            .ToArray();

        return new string(characters).Trim('-');
    }

    private static bool IsAllowedTargetPath(string path)
    {
        if (path.StartsWith("/", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(path, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
