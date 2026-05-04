namespace WoongBlog.Infrastructure.LoadTesting;

public interface IRealLoadTestRunRegistry
{
    bool TryAddRun(RealLoadTestRunEntry run, out string? conflictReason);

    bool TryGetRun(string runId, out RealLoadTestRunEntry? run);
}
