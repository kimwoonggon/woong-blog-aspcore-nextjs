using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed class LoadTestDbConnectionDiagnosticsInterceptor : DbConnectionInterceptor
{
    private readonly IDatabaseDiagnosticsCollector _collector;
    private readonly ConcurrentDictionary<DbConnection, long> _openingSince = new();

    public LoadTestDbConnectionDiagnosticsInterceptor(IDatabaseDiagnosticsCollector collector)
    {
        _collector = collector;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        _openingSince[connection] = Stopwatch.GetTimestamp();
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        _openingSince[connection] = Stopwatch.GetTimestamp();
        return ValueTask.FromResult(result);
    }

    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        var duration = ResolveDuration(connection, eventData.Duration);
        _collector.RecordConnectionOpen(duration);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var duration = ResolveDuration(connection, eventData.Duration);
        _collector.RecordConnectionOpen(duration);
        return Task.CompletedTask;
    }

    public override void ConnectionFailed(
        DbConnection connection,
        ConnectionErrorEventData eventData)
    {
        var duration = ResolveDuration(connection, eventData.Duration);
        _collector.RecordConnectionOpen(duration, eventData.Exception);
    }

    public override Task ConnectionFailedAsync(
        DbConnection connection,
        ConnectionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var duration = ResolveDuration(connection, eventData.Duration);
        _collector.RecordConnectionOpen(duration, eventData.Exception);
        return Task.CompletedTask;
    }

    private TimeSpan ResolveDuration(DbConnection connection, TimeSpan efDuration)
    {
        if (_openingSince.TryRemove(connection, out var startTicks))
        {
            return Stopwatch.GetElapsedTime(startTicks);
        }

        return efDuration;
    }
}
