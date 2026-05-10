namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public interface IRequestDatabaseDiagnostics
{
    IRequestDatabaseDiagnosticsScope BeginRequest();

    void RecordCommand(TimeSpan duration);

    RequestDatabaseDiagnosticsSnapshot CaptureCurrent();
}

public interface IRequestDatabaseDiagnosticsScope : IDisposable
{
    RequestDatabaseDiagnosticsSnapshot CaptureSnapshot();
}

public sealed record RequestDatabaseDiagnosticsSnapshot(
    long CommandCount,
    double CommandElapsedMs);
