namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed record DatabaseDiagnosticsMetricsSnapshot(
    DatabaseLatencyStats CommandLatency,
    DatabaseLatencyStats ConnectionOpenLatency,
    long SlowQueryCount,
    IReadOnlyList<DatabaseSlowQuerySample> RecentSlowQueries,
    long TimeoutCount,
    long ErrorCount);

public sealed record DatabaseLatencyStats(
    int SampleCount,
    double? P50Ms,
    double? P95Ms,
    double? P99Ms);

public sealed record DatabaseSlowQuerySample(
    DateTimeOffset CapturedAt,
    double DurationMs,
    string SqlPreview,
    string? ErrorCategory);
