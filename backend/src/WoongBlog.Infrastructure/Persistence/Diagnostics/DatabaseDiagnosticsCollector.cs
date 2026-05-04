using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed class DatabaseDiagnosticsCollector : IDatabaseDiagnosticsCollector
{
    private static readonly Regex StringLiteralRegex = new("'([^']|'')*'", RegexOptions.Compiled);
    private static readonly Regex NumberLiteralRegex = new(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly FixedSizeDoubleRingBuffer _commandLatencySamples;
    private readonly FixedSizeDoubleRingBuffer _connectionLatencySamples;
    private readonly FixedSizeRingBuffer<DatabaseSlowQuerySample> _slowQuerySamples;
    private readonly double _slowQueryThresholdMs;

    private long _slowQueryCount;
    private long _timeoutCount;
    private long _errorCount;

    public DatabaseDiagnosticsCollector(IOptions<DatabaseDiagnosticsOptions> options)
    {
        var value = options.Value;
        var latencyCapacity = Math.Max(32, value.LatencySampleCapacity);
        var slowQueryCapacity = Math.Max(8, value.SlowQuerySampleCapacity);

        _commandLatencySamples = new FixedSizeDoubleRingBuffer(latencyCapacity);
        _connectionLatencySamples = new FixedSizeDoubleRingBuffer(latencyCapacity);
        _slowQuerySamples = new FixedSizeRingBuffer<DatabaseSlowQuerySample>(slowQueryCapacity);
        _slowQueryThresholdMs = Math.Max(1, value.SlowQueryThresholdMs);
    }

    public void RecordCommand(TimeSpan duration, string? commandText, Exception? exception = null)
    {
        var elapsedMs = Math.Max(0, duration.TotalMilliseconds);
        _commandLatencySamples.Add(elapsedMs);
        RecordError(exception);

        if (elapsedMs < _slowQueryThresholdMs)
        {
            return;
        }

        Interlocked.Increment(ref _slowQueryCount);
        _slowQuerySamples.Add(new DatabaseSlowQuerySample(
            DateTimeOffset.UtcNow,
            Math.Round(elapsedMs, 1),
            SanitizeSqlPreview(commandText),
            CategorizeError(exception)));
    }

    public void RecordConnectionOpen(TimeSpan duration, Exception? exception = null)
    {
        _connectionLatencySamples.Add(Math.Max(0, duration.TotalMilliseconds));
        RecordError(exception);
    }

    public DatabaseDiagnosticsMetricsSnapshot CaptureSnapshot()
    {
        var commandSnapshot = _commandLatencySamples.Snapshot();
        var connectionSnapshot = _connectionLatencySamples.Snapshot();

        return new DatabaseDiagnosticsMetricsSnapshot(
            ToStats(commandSnapshot),
            ToStats(connectionSnapshot),
            Volatile.Read(ref _slowQueryCount),
            _slowQuerySamples.Snapshot(),
            Volatile.Read(ref _timeoutCount),
            Volatile.Read(ref _errorCount));
    }

    private void RecordError(Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        Interlocked.Increment(ref _errorCount);
        if (IsTimeoutLike(exception))
        {
            Interlocked.Increment(ref _timeoutCount);
        }
    }

    private static DatabaseLatencyStats ToStats(double[] samples)
    {
        if (samples.Length == 0)
        {
            return new DatabaseLatencyStats(0, null, null, null);
        }

        Array.Sort(samples);

        return new DatabaseLatencyStats(
            samples.Length,
            Math.Round(GetPercentile(samples, 0.50), 1),
            Math.Round(GetPercentile(samples, 0.95), 1),
            Math.Round(GetPercentile(samples, 0.99), 1));
    }

    private static double GetPercentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var rank = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        var index = Math.Clamp(rank, 0, sorted.Length - 1);
        return sorted[index];
    }

    private static string SanitizeSqlPreview(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return "N/A";
        }

        var collapsed = WhitespaceRegex.Replace(sql, " ");
        var withoutStrings = StringLiteralRegex.Replace(collapsed, "'?'");
        var withoutNumbers = NumberLiteralRegex.Replace(withoutStrings, "?");
        var trimmed = withoutNumbers.Trim();
        if (trimmed.Length <= 220)
        {
            return trimmed;
        }

        return $"{trimmed[..220]}...";
    }

    private static string? CategorizeError(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        if (IsTimeoutLike(exception))
        {
            return "timeout";
        }

        return "error";
    }

    private static bool IsTimeoutLike(Exception exception)
    {
        return exception is TimeoutException
            || exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FixedSizeDoubleRingBuffer
    {
        private readonly double[] _buffer;
        private readonly object _sync = new();
        private int _nextIndex;
        private int _count;

        public FixedSizeDoubleRingBuffer(int capacity)
        {
            _buffer = new double[capacity];
        }

        public void Add(double value)
        {
            lock (_sync)
            {
                _buffer[_nextIndex] = value;
                _nextIndex = (_nextIndex + 1) % _buffer.Length;
                if (_count < _buffer.Length)
                {
                    _count++;
                }
            }
        }

        public double[] Snapshot()
        {
            lock (_sync)
            {
                var result = new double[_count];
                if (_count == 0)
                {
                    return result;
                }

                var start = _count == _buffer.Length ? _nextIndex : 0;
                for (var index = 0; index < _count; index++)
                {
                    result[index] = _buffer[(start + index) % _buffer.Length];
                }

                return result;
            }
        }
    }

    private sealed class FixedSizeRingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly object _sync = new();
        private int _nextIndex;
        private int _count;

        public FixedSizeRingBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        public void Add(T value)
        {
            lock (_sync)
            {
                _buffer[_nextIndex] = value;
                _nextIndex = (_nextIndex + 1) % _buffer.Length;
                if (_count < _buffer.Length)
                {
                    _count++;
                }
            }
        }

        public IReadOnlyList<T> Snapshot()
        {
            lock (_sync)
            {
                if (_count == 0)
                {
                    return Array.Empty<T>();
                }

                var result = new List<T>(_count);
                var start = _count == _buffer.Length ? _nextIndex : 0;
                for (var index = 0; index < _count; index++)
                {
                    result.Add(_buffer[(start + index) % _buffer.Length]);
                }

                return result;
            }
        }
    }
}
