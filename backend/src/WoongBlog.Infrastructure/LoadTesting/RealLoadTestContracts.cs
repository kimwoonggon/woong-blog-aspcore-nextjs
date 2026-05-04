namespace WoongBlog.Infrastructure.LoadTesting;

public static class RealLoadTestRunStates
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Stopped = "stopped";
}

public sealed record RealLoadTestStartRequest(
    string Scenario,
    string Runner,
    string Target,
    int Rate,
    int DurationSeconds,
    int MaxVus);

public sealed record RealLoadTestStartResponse(
    string RunId,
    string Status,
    string Runner,
    string Scenario,
    DateTimeOffset StartedAtUtc);

public sealed record RealLoadTestStatusResponse(
    string RunId,
    string Status,
    string Runner,
    string Scenario,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    double ElapsedSeconds,
    long TotalRequests,
    long FailedRequests,
    double ErrorRate,
    double CurrentRps,
    double AverageRps,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    IReadOnlyDictionary<string, long> StatusCounts);

public sealed record RealLoadTestMetricsResponse(
    string RunId,
    string Status,
    long TotalRequests,
    long FailedRequests,
    double CurrentRps,
    double AverageRps,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    IReadOnlyDictionary<string, long> StatusCounts,
    IReadOnlyList<RealLoadTestMetricPoint> Metrics);

public sealed record RealLoadTestMetricPoint(
    DateTimeOffset TimestampUtc,
    double ElapsedSeconds,
    long TotalRequests,
    long FailedRequests,
    double CurrentRps,
    double AverageRps,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    IReadOnlyDictionary<string, long> StatusCounts);

public sealed record RealLoadTestStopResponse(
    string RunId,
    string Status,
    DateTimeOffset? StoppedAtUtc);

public sealed class RealLoadTestRunEntry
{
    public RealLoadTestRunEntry(
        string runId,
        string runner,
        string scenario,
        string target,
        int rate,
        int durationSeconds,
        int maxVus,
        DateTimeOffset startedAtUtc)
    {
        RunId = runId;
        Runner = runner;
        Scenario = scenario;
        Target = target;
        Rate = rate;
        DurationSeconds = durationSeconds;
        MaxVus = maxVus;
        StartedAtUtc = startedAtUtc;
        Status = RealLoadTestRunStates.Queued;
        CancellationTokenSource = new CancellationTokenSource();
    }

    public object SyncRoot { get; } = new();

    public string RunId { get; }

    public string Runner { get; }

    public string Scenario { get; }

    public string Target { get; }

    public int Rate { get; }

    public int DurationSeconds { get; }

    public int MaxVus { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public string Status { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }

    public long TotalRequests { get; set; }

    public long FailedRequests { get; set; }

    public double CurrentRps { get; set; }

    public double AverageRps { get; set; }

    public double P50Ms { get; set; }

    public double P95Ms { get; set; }

    public double P99Ms { get; set; }

    public double MaxMs { get; set; }

    public Dictionary<string, long> StatusCounts { get; } = CreateStatusCounts();

    public CancellationTokenSource CancellationTokenSource { get; }

    public RealLoadTestStatusResponse ToStatusResponse(DateTimeOffset nowUtc)
    {
        var end = EndedAtUtc ?? nowUtc;
        var elapsedSeconds = Math.Max(0, (end - StartedAtUtc).TotalSeconds);
        var errorRate = TotalRequests == 0 ? 0 : Math.Round((double)FailedRequests / TotalRequests, 6);

        return new RealLoadTestStatusResponse(
            RunId,
            Status,
            Runner,
            Scenario,
            StartedAtUtc,
            EndedAtUtc,
            elapsedSeconds,
            TotalRequests,
            FailedRequests,
            errorRate,
            CurrentRps,
            AverageRps,
            P50Ms,
            P95Ms,
            P99Ms,
            MaxMs,
            new Dictionary<string, long>(StatusCounts, StringComparer.OrdinalIgnoreCase));
    }

    public static Dictionary<string, long> CreateStatusCounts()
    {
        return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["2xx"] = 0,
            ["3xx"] = 0,
            ["4xx"] = 0,
            ["5xx"] = 0,
            ["429"] = 0,
            ["503"] = 0,
            ["timeout"] = 0
        };
    }
}

public sealed class RealLoadTestValidationException(string message) : Exception(message);

public sealed class RealLoadTestConflictException(string message) : Exception(message);

public sealed class RealLoadTestNotFoundException(string message) : Exception(message);
