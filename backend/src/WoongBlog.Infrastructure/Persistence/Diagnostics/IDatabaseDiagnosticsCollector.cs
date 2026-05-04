namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public interface IDatabaseDiagnosticsCollector
{
    void RecordCommand(TimeSpan duration, string? commandText, Exception? exception = null);

    void RecordConnectionOpen(TimeSpan duration, Exception? exception = null);

    DatabaseDiagnosticsMetricsSnapshot CaptureSnapshot();
}
