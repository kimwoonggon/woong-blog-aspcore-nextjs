using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class InMemoryRealLoadTestRunRegistry(IOptions<LoadTestingOptions> options) : IRealLoadTestRunRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<string, RealLoadTestRunEntry> _runs = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxConcurrentRuns = Math.Max(1, options.Value.MaxConcurrentRuns);

    public bool TryAddRun(RealLoadTestRunEntry run, out string? conflictReason)
    {
        lock (_sync)
        {
            var activeRuns = _runs.Values.Count(static entry =>
                string.Equals(entry.Status, RealLoadTestRunStates.Queued, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.Status, RealLoadTestRunStates.Running, StringComparison.OrdinalIgnoreCase));

            if (activeRuns >= _maxConcurrentRuns)
            {
                conflictReason = "another run is already active";
                return false;
            }

            _runs[run.RunId] = run;
            conflictReason = null;
            return true;
        }
    }

    public bool TryGetRun(string runId, out RealLoadTestRunEntry? run)
    {
        lock (_sync)
        {
            return _runs.TryGetValue(runId, out run);
        }
    }
}
