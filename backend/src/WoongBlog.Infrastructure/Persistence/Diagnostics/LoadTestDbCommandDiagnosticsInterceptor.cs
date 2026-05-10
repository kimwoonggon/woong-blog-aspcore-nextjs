using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed class LoadTestDbCommandDiagnosticsInterceptor : DbCommandInterceptor
{
    private readonly IDatabaseDiagnosticsCollector _collector;
    private readonly IRequestDatabaseDiagnostics? _requestDiagnostics;

    public LoadTestDbCommandDiagnosticsInterceptor(
        IDatabaseDiagnosticsCollector collector,
        IRequestDatabaseDiagnostics? requestDiagnostics = null)
    {
        _collector = collector;
        _requestDiagnostics = requestDiagnostics;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RecordCommand(eventData.Duration, command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        RecordCommand(eventData.Duration, command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        RecordCommand(eventData.Duration, command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        RecordCommand(eventData.Duration, command.CommandText, eventData.Exception);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RecordCommand(eventData.Duration, command.CommandText, eventData.Exception);
        return Task.CompletedTask;
    }

    private void RecordCommand(TimeSpan duration, string? commandText, Exception? exception = null)
    {
        _collector.RecordCommand(duration, commandText, exception);
        _requestDiagnostics?.RecordCommand(duration);
    }
}
