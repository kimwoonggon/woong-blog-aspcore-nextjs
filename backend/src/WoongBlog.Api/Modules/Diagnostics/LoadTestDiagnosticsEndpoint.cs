using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Persistence.Diagnostics;

namespace WoongBlog.Api.Modules.Diagnostics;

internal static class LoadTestDiagnosticsEndpoint
{
    private const string Path = "/api/admin/load-test/diagnostics";

    internal static void MapLoadTestDiagnostics(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                Path,
                async (WoongBlogDbContext dbContext, IDatabaseDiagnosticsCollector collector, CancellationToken cancellationToken) =>
                {
                    var database = await CaptureDatabaseDiagnosticsSafelyAsync(dbContext, collector, cancellationToken);

                    return Results.Ok(new LoadTestDiagnosticsResponse(
                        DateTimeOffset.UtcNow,
                        CaptureProcessDiagnostics(),
                        CaptureGcDiagnostics(),
                        CaptureThreadPoolDiagnostics(),
                        database));
                })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("GetLoadTestDiagnostics")
            .Produces<LoadTestDiagnosticsResponse>(StatusCodes.Status200OK);
    }

    private static ProcessDiagnostics CaptureProcessDiagnostics()
    {
        using var process = Process.GetCurrentProcess();

        return new ProcessDiagnostics(
            process.WorkingSet64,
            Environment.ProcessorCount);
    }

    private static GcDiagnostics CaptureGcDiagnostics()
    {
        var memoryInfo = GC.GetGCMemoryInfo();

        return new GcDiagnostics(
            memoryInfo.HeapSizeBytes,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            memoryInfo.PauseTimePercentage);
    }

    private static ThreadPoolDiagnostics CaptureThreadPoolDiagnostics()
    {
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);

        return new ThreadPoolDiagnostics(
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount,
            ThreadPool.CompletedWorkItemCount,
            availableWorkerThreads,
            maxWorkerThreads);
    }

    private static async Task<DatabaseDiagnostics> CaptureDatabaseDiagnosticsSafelyAsync(
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
            return DatabaseDiagnostics.FromError(
                fallback,
                "collector_failure",
                exception.Message);
        }
    }

    private static async Task<DatabaseDiagnostics> CaptureDatabaseDiagnosticsAsync(
        WoongBlogDbContext dbContext,
        IDatabaseDiagnosticsCollector collector,
        CancellationToken cancellationToken)
    {
        var snapshot = collector.CaptureSnapshot();

        if (!dbContext.Database.IsRelational())
        {
            return DatabaseDiagnostics.Unavailable(snapshot);
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

            return DatabaseDiagnostics.Available(
                snapshot,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1),
                connectionCounts);
        }
        catch (Exception exception)
        {
            snapshot = TryCaptureSnapshotOrFallback(collector);
            var category = IsTimeoutLike(exception) ? "timeout" : "probe_failure";

            return DatabaseDiagnostics.FromError(snapshot, category, exception.Message);
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

    private static async Task<PostgresConnectionCounts> TryCapturePostgresConnectionCountsAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        if (!connection.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            return new PostgresConnectionCounts(null, null, null, null);
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
            return new PostgresConnectionCounts(null, null, null, null);
        }

        return new PostgresConnectionCounts(
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

    private sealed record PostgresConnectionCounts(
        int? OpenConnections,
        int? ActiveConnections,
        int? IdleConnections,
        int? IdleInTransactionConnections);

    private sealed record LoadTestDiagnosticsResponse(
        DateTimeOffset Timestamp,
        ProcessDiagnostics Process,
        GcDiagnostics Gc,
        ThreadPoolDiagnostics ThreadPool,
        DatabaseDiagnostics Database);

    private sealed record ProcessDiagnostics(
        long MemoryBytes,
        int ProcessorCount);

    private sealed record GcDiagnostics(
        long HeapSizeBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        double TimeInGcPercent);

    private sealed record ThreadPoolDiagnostics(
        int WorkerThreads,
        long PendingWorkItemCount,
        long CompletedWorkItemCount,
        int AvailableWorkerThreads,
        int MaxWorkerThreads);

    private sealed record DatabaseDiagnostics(
        string Status,
        double? LatencyMs,
        int? OpenConnections,
        int? ActiveConnections,
        int? IdleConnections,
        int? IdleInTransactionConnections,
        DatabaseLatencyView CommandLatency,
        DatabaseLatencyView ConnectionOpenLatency,
        long SlowQueryCount,
        IReadOnlyList<SlowQueryView> RecentSlowQueries,
        long TimeoutCount,
        long ErrorCount,
        string? Error,
        string? ErrorCategory)
    {
        internal static DatabaseDiagnostics Unavailable(DatabaseDiagnosticsMetricsSnapshot snapshot) =>
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

        internal static DatabaseDiagnostics Available(
            DatabaseDiagnosticsMetricsSnapshot snapshot,
            double latencyMs,
            PostgresConnectionCounts connectionCounts) =>
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

        internal static DatabaseDiagnostics FromError(
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

        private static DatabaseLatencyView ToLatencyView(DatabaseLatencyStats stats) =>
            new(stats.SampleCount, stats.P50Ms, stats.P95Ms, stats.P99Ms);

        private static IReadOnlyList<SlowQueryView> ToSlowQueryViews(IReadOnlyList<DatabaseSlowQuerySample> samples)
        {
            if (samples.Count == 0)
            {
                return Array.Empty<SlowQueryView>();
            }

            return samples.Select(sample => new SlowQueryView(
                sample.CapturedAt,
                sample.DurationMs,
                sample.SqlPreview,
                sample.ErrorCategory)).ToArray();
        }
    }

    private sealed record DatabaseLatencyView(
        int SampleCount,
        double? P50Ms,
        double? P95Ms,
        double? P99Ms);

    private sealed record SlowQueryView(
        DateTimeOffset CapturedAt,
        double DurationMs,
        string SqlPreview,
        string? ErrorCategory);
}
