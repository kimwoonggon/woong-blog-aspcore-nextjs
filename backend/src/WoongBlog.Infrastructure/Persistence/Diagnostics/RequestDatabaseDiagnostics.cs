namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed class RequestDatabaseDiagnostics : IRequestDatabaseDiagnostics
{
    private static readonly RequestDatabaseDiagnosticsSnapshot EmptySnapshot = new(0, 0);
    private readonly AsyncLocal<RequestDatabaseDiagnosticsScope?> _current = new();

    public IRequestDatabaseDiagnosticsScope BeginRequest()
    {
        var scope = new RequestDatabaseDiagnosticsScope(_current.Value, value => _current.Value = value);
        _current.Value = scope;
        return scope;
    }

    public void RecordCommand(TimeSpan duration)
    {
        _current.Value?.RecordCommand(duration);
    }

    public RequestDatabaseDiagnosticsSnapshot CaptureCurrent()
    {
        return _current.Value?.CaptureSnapshot() ?? EmptySnapshot;
    }

    private sealed class RequestDatabaseDiagnosticsScope : IRequestDatabaseDiagnosticsScope
    {
        private readonly RequestDatabaseDiagnosticsScope? _parent;
        private readonly Action<RequestDatabaseDiagnosticsScope?> _restoreCurrent;
        private long _commandCount;
        private long _commandElapsedTicks;
        private bool _disposed;

        public RequestDatabaseDiagnosticsScope(
            RequestDatabaseDiagnosticsScope? parent,
            Action<RequestDatabaseDiagnosticsScope?> restoreCurrent)
        {
            _parent = parent;
            _restoreCurrent = restoreCurrent;
        }

        public void RecordCommand(TimeSpan duration)
        {
            if (_disposed)
            {
                return;
            }

            Interlocked.Increment(ref _commandCount);
            Interlocked.Add(ref _commandElapsedTicks, Math.Max(0, duration.Ticks));
        }

        public RequestDatabaseDiagnosticsSnapshot CaptureSnapshot()
        {
            var elapsedTicks = Volatile.Read(ref _commandElapsedTicks);
            return new RequestDatabaseDiagnosticsSnapshot(
                Volatile.Read(ref _commandCount),
                Math.Round(TimeSpan.FromTicks(elapsedTicks).TotalMilliseconds, 1));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _restoreCurrent(_parent);
        }
    }
}
