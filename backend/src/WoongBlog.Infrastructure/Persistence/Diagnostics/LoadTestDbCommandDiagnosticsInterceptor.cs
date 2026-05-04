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

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText);
        return result;
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        _collector.RecordCommand(eventData.Duration, command.CommandText, eventData.Exception);
    }
}
