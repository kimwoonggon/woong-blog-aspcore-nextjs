using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Persistence.Diagnostics;

namespace WoongBlog.Infrastructure.LoadTesting;

public interface ILoadTestDiagnosticsSampler
{
    Task<LoadTestDiagnosticsSnapshot> CaptureAsync(CancellationToken cancellationToken);
}

public sealed class LoadTestDiagnosticsSampler(IServiceScopeFactory serviceScopeFactory) : ILoadTestDiagnosticsSampler
{
    public async Task<LoadTestDiagnosticsSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var collector = scope.ServiceProvider.GetRequiredService<IDatabaseDiagnosticsCollector>();
        var database = await CaptureDatabaseDiagnosticsSafelyAsync(dbContext, collector, cancellationToken);

        return new LoadTestDiagnosticsSnapshot(
            DateTimeOffset.UtcNow,
            CaptureProcessDiagnostics(),
            CaptureGcDiagnostics(),
            CaptureThreadPoolDiagnostics(),
            database);
    }

    private static LoadTestProcessDiagnostics CaptureProcessDiagnostics()
    {
        using var process = Process.GetCurrentProcess();
        return new LoadTestProcessDiagnostics(
            process.WorkingSet64,
            Environment.ProcessorCount);
    }

    private static LoadTestGcDiagnostics CaptureGcDiagnostics()
    {
        var memoryInfo = GC.GetGCMemoryInfo();
        return new LoadTestGcDiagnostics(
            memoryInfo.HeapSizeBytes,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            memoryInfo.PauseTimePercentage);
    }

    private static LoadTestThreadPoolDiagnostics CaptureThreadPoolDiagnostics()
    {
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);

        return new LoadTestThreadPoolDiagnostics(
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount,
            ThreadPool.CompletedWorkItemCount,
            availableWorkerThreads,
            maxWorkerThreads);
    }

    private static async Task<LoadTestDatabaseDiagnostics> CaptureDatabaseDiagnosticsSafelyAsync(
        WoongBlogDbContext dbContext,
        IDatabaseDiagnosticsCollector collector,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CaptureDatabaseDiagnosticsAsync(dbContext, collector, cancellationToken);
        }
        catch (Exception exception)
        {
            var fallback = TryCaptureSnapshotOrFallback(collector);
            return LoadTestDatabaseDiagnostics.FromError(
                fallback,
                "collector_failure",
                exception.Message);
        }
    }

    private static async Task<LoadTestDatabaseDiagnostics> CaptureDatabaseDiagnosticsAsync(
        WoongBlogDbContext dbContext,
        IDatabaseDiagnosticsCollector collector,
        CancellationToken cancellationToken)
    {
        var snapshot = collector.CaptureSnapshot();

        if (!dbContext.Database.IsRelational())
        {
            return LoadTestDatabaseDiagnostics.Unavailable(snapshot);
        }

        var stopwatch = Stopwatch.StartNew();
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        try
        {
            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using (var pingCommand = connection.CreateCommand())
            {
                pingCommand.CommandText = "SELECT 1";
                _ = await pingCommand.ExecuteScalarAsync(cancellationToken);
            }

            stopwatch.Stop();
            snapshot = collector.CaptureSnapshot();
            var connectionCounts = await TryCapturePostgresConnectionCountsAsync(connection, cancellationToken);

            return LoadTestDatabaseDiagnostics.Available(
                snapshot,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1),
                connectionCounts);
        }
        catch (Exception exception)
        {
            snapshot = TryCaptureSnapshotOrFallback(collector);
            var category = IsTimeoutLike(exception) ? "timeout" : "probe_failure";

            return LoadTestDatabaseDiagnostics.FromError(snapshot, category, exception.Message);
        }
        finally
        {
            if (shouldClose && connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static DatabaseDiagnosticsMetricsSnapshot TryCaptureSnapshotOrFallback(IDatabaseDiagnosticsCollector collector)
    {
        try
        {
            return collector.CaptureSnapshot();
        }
        catch
        {
            return new DatabaseDiagnosticsMetricsSnapshot(
                new DatabaseLatencyStats(0, null, null, null),
                new DatabaseLatencyStats(0, null, null, null),
                0,
                Array.Empty<DatabaseSlowQuerySample>(),
                0,
                0);
        }
    }

    private static async Task<LoadTestPostgresConnectionCounts> TryCapturePostgresConnectionCountsAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        if (!connection.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            return new LoadTestPostgresConnectionCounts(null, null, null, null);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COUNT(*)::int AS open_connections,
                COUNT(*) FILTER (WHERE state = 'active')::int AS active_connections,
                COUNT(*) FILTER (WHERE state = 'idle')::int AS idle_connections,
                COUNT(*) FILTER (WHERE state = 'idle in transaction')::int AS idle_in_transaction_connections
            FROM pg_stat_activity
            WHERE datname = current_database();
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new LoadTestPostgresConnectionCounts(null, null, null, null);
        }

        return new LoadTestPostgresConnectionCounts(
            reader.IsDBNull(0) ? null : reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetInt32(3));
    }

    private static bool IsTimeoutLike(Exception exception)
    {
        return exception is TimeoutException
            || exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record LoadTestDiagnosticsSnapshot(
    DateTimeOffset Timestamp,
    LoadTestProcessDiagnostics Process,
    LoadTestGcDiagnostics Gc,
    LoadTestThreadPoolDiagnostics ThreadPool,
    LoadTestDatabaseDiagnostics Database);

public sealed record LoadTestProcessDiagnostics(
    long MemoryBytes,
    int ProcessorCount);

public sealed record LoadTestGcDiagnostics(
    long HeapSizeBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    double TimeInGcPercent);

public sealed record LoadTestThreadPoolDiagnostics(
    int WorkerThreads,
    long PendingWorkItemCount,
    long CompletedWorkItemCount,
    int AvailableWorkerThreads,
    int MaxWorkerThreads);

public sealed record LoadTestDatabaseDiagnostics(
    string Status,
    double? LatencyMs,
    int? OpenConnections,
    int? ActiveConnections,
    int? IdleConnections,
    int? IdleInTransactionConnections,
    LoadTestDatabaseLatencyView CommandLatency,
    LoadTestDatabaseLatencyView ConnectionOpenLatency,
    long SlowQueryCount,
    IReadOnlyList<LoadTestSlowQueryView> RecentSlowQueries,
    long TimeoutCount,
    long ErrorCount,
    string? Error,
    string? ErrorCategory)
{
    internal static LoadTestDatabaseDiagnostics Unavailable(DatabaseDiagnosticsMetricsSnapshot snapshot) =>
        new(
            "unavailable",
            null,
            null,
            null,
            null,
            null,
            ToLatencyView(snapshot.CommandLatency),
            ToLatencyView(snapshot.ConnectionOpenLatency),
            snapshot.SlowQueryCount,
            ToSlowQueryViews(snapshot.RecentSlowQueries),
            snapshot.TimeoutCount,
            snapshot.ErrorCount,
            null,
            null);

    internal static LoadTestDatabaseDiagnostics Available(
        DatabaseDiagnosticsMetricsSnapshot snapshot,
        double latencyMs,
        LoadTestPostgresConnectionCounts connectionCounts) =>
        new(
            "available",
            latencyMs,
            connectionCounts.OpenConnections,
            connectionCounts.ActiveConnections,
            connectionCounts.IdleConnections,
            connectionCounts.IdleInTransactionConnections,
            ToLatencyView(snapshot.CommandLatency),
            ToLatencyView(snapshot.ConnectionOpenLatency),
            snapshot.SlowQueryCount,
            ToSlowQueryViews(snapshot.RecentSlowQueries),
            snapshot.TimeoutCount,
            snapshot.ErrorCount,
            null,
            null);

    internal static LoadTestDatabaseDiagnostics FromError(
        DatabaseDiagnosticsMetricsSnapshot snapshot,
        string errorCategory,
        string error) =>
        new(
            "error",
            null,
            null,
            null,
            null,
            null,
            ToLatencyView(snapshot.CommandLatency),
            ToLatencyView(snapshot.ConnectionOpenLatency),
            snapshot.SlowQueryCount,
            ToSlowQueryViews(snapshot.RecentSlowQueries),
            snapshot.TimeoutCount,
            snapshot.ErrorCount,
            error,
            errorCategory);

    private static LoadTestDatabaseLatencyView ToLatencyView(DatabaseLatencyStats stats) =>
        new(stats.SampleCount, stats.P50Ms, stats.P95Ms, stats.P99Ms);

    private static IReadOnlyList<LoadTestSlowQueryView> ToSlowQueryViews(IReadOnlyList<DatabaseSlowQuerySample> samples)
    {
        if (samples.Count == 0)
        {
            return Array.Empty<LoadTestSlowQueryView>();
        }

        return samples.Select(sample => new LoadTestSlowQueryView(
            sample.CapturedAt,
            sample.DurationMs,
            sample.SqlPreview,
            sample.ErrorCategory)).ToArray();
    }
}

public sealed record LoadTestDatabaseLatencyView(
    int SampleCount,
    double? P50Ms,
    double? P95Ms,
    double? P99Ms);

public sealed record LoadTestSlowQueryView(
    DateTimeOffset CapturedAt,
    double DurationMs,
    string SqlPreview,
    string? ErrorCategory);

internal sealed record LoadTestPostgresConnectionCounts(
    int? OpenConnections,
    int? ActiveConnections,
    int? IdleConnections,
    int? IdleInTransactionConnections);
