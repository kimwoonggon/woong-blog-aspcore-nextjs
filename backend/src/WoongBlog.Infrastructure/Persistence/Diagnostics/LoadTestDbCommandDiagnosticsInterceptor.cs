using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed class LoadTestDbCommandDiagnosticsInterceptor : DbCommandInterceptor
{
    private readonly IDatabaseDiagnosticsCollector _collector;

    public LoadTestDbCommandDiagnosticsInterceptor(IDatabaseDiagnosticsCollector collector)
    {
        _collector = collector;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText, eventData.Exception);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText, eventData.Exception);
        return Task.CompletedTask;
    }
}
