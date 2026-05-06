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

public sealed class LoadTestDiagnosticsSampler(
    IServiceScopeFactory serviceScopeFactory,
    PersistenceRuntimeDiagnostics persistenceRuntimeDiagnostics) : ILoadTestDiagnosticsSampler
{
    public async Task<LoadTestDiagnosticsSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var collector = scope.ServiceProvider.GetRequiredService<IDatabaseDiagnosticsCollector>();
        var database = await CaptureDatabaseDiagnosticsSafelyAsync(
            dbContext,
            collector,
            persistenceRuntimeDiagnostics,
            cancellationToken);

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
            Environment.ProcessorCount,
            TryReadMemoryLimitBytes(),
            TryReadCpuQuotaCores());
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
        PersistenceRuntimeDiagnostics persistenceRuntimeDiagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CaptureDatabaseDiagnosticsAsync(
                dbContext,
                collector,
                persistenceRuntimeDiagnostics,
                cancellationToken);
        }
        catch (Exception exception)
        {
            var fallback = TryCaptureSnapshotOrFallback(collector);
            return LoadTestDatabaseDiagnostics.FromError(
                fallback,
                ToPoolDiagnostics(persistenceRuntimeDiagnostics),
                "collector_failure",
                exception.Message);
        }
    }

    private static async Task<LoadTestDatabaseDiagnostics> CaptureDatabaseDiagnosticsAsync(
        WoongBlogDbContext dbContext,
        IDatabaseDiagnosticsCollector collector,
        PersistenceRuntimeDiagnostics persistenceRuntimeDiagnostics,
        CancellationToken cancellationToken)
    {
        var snapshot = collector.CaptureSnapshot();
        var poolDiagnostics = ToPoolDiagnostics(persistenceRuntimeDiagnostics);

        if (!dbContext.Database.IsRelational())
        {
            return LoadTestDatabaseDiagnostics.Unavailable(snapshot, poolDiagnostics);
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
                poolDiagnostics,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1),
                connectionCounts);
        }
        catch (Exception exception)
        {
            snapshot = TryCaptureSnapshotOrFallback(collector);
            var category = IsTimeoutLike(exception) ? "timeout" : "probe_failure";

            return LoadTestDatabaseDiagnostics.FromError(snapshot, poolDiagnostics, category, exception.Message);
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

    private static LoadTestDatabasePoolDiagnostics ToPoolDiagnostics(PersistenceRuntimeDiagnostics diagnostics)
    {
        return new LoadTestDatabasePoolDiagnostics(
            diagnostics.DatabaseProvider,
            diagnostics.DbContextPoolSize,
            diagnostics.NpgsqlMinimumPoolSize,
            diagnostics.NpgsqlMaximumPoolSize,
            diagnostics.NpgsqlPoolLimitSource);
    }

    private static long? TryReadMemoryLimitBytes()
    {
        return TryReadCgroupInt64("/sys/fs/cgroup/memory.max")
            ?? TryReadCgroupInt64("/sys/fs/cgroup/memory/memory.limit_in_bytes");
    }

    private static double? TryReadCpuQuotaCores()
    {
        var cpuMax = TryReadText("/sys/fs/cgroup/cpu.max");
        if (!string.IsNullOrWhiteSpace(cpuMax))
        {
            var parts = cpuMax.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2
                && !string.Equals(parts[0], "max", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[0], out var quota)
                && long.TryParse(parts[1], out var period))
            {
                return CalculateCpuQuotaCores(quota, period);
            }
        }

        var legacyQuota = TryReadCgroupInt64("/sys/fs/cgroup/cpu/cpu.cfs_quota_us");
        var legacyPeriod = TryReadCgroupInt64("/sys/fs/cgroup/cpu/cpu.cfs_period_us");
        legacyQuota ??= TryReadCgroupInt64("/sys/fs/cgroup/cpu,cpuacct/cpu.cfs_quota_us");
        legacyPeriod ??= TryReadCgroupInt64("/sys/fs/cgroup/cpu,cpuacct/cpu.cfs_period_us");

        return legacyQuota.HasValue && legacyPeriod.HasValue
            ? CalculateCpuQuotaCores(legacyQuota.Value, legacyPeriod.Value)
            : null;
    }

    private static double? CalculateCpuQuotaCores(long quota, long period)
    {
        if (quota <= 0 || period <= 0)
        {
            return null;
        }

        return Math.Round(quota / (double)period, 2);
    }

    private static long? TryReadCgroupInt64(string path)
    {
        var value = TryReadText(path);
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), "max", StringComparison.OrdinalIgnoreCase)
            || !long.TryParse(value.Trim(), out var parsed)
            || parsed <= 0)
        {
            return null;
        }

        return parsed >= (1L << 60) ? null : parsed;
    }

    private static string? TryReadText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
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
    int ProcessorCount,
    long? MemoryLimitBytes = null,
    double? CpuQuotaCores = null);

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
    string? ErrorCategory,
    LoadTestDatabasePoolDiagnostics? Pool = null)
{
    internal static LoadTestDatabaseDiagnostics Unavailable(
        DatabaseDiagnosticsMetricsSnapshot snapshot,
        LoadTestDatabasePoolDiagnostics pool) =>
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
            null,
            pool);

    internal static LoadTestDatabaseDiagnostics Available(
        DatabaseDiagnosticsMetricsSnapshot snapshot,
        LoadTestDatabasePoolDiagnostics pool,
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
            null,
            pool);

    internal static LoadTestDatabaseDiagnostics FromError(
        DatabaseDiagnosticsMetricsSnapshot snapshot,
        LoadTestDatabasePoolDiagnostics pool,
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
            errorCategory,
            pool);

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

public sealed record LoadTestDatabasePoolDiagnostics(
    string DatabaseProvider,
    int DbContextPoolSize,
    int? NpgsqlMinimumPoolSize,
    int? NpgsqlMaximumPoolSize,
    string NpgsqlPoolLimitSource);

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
