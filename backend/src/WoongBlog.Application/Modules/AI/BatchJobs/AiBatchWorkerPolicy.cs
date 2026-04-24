namespace WoongBlog.Application.Modules.AI.BatchJobs;

public static class AiBatchWorkerPolicy
{
    public static int ResolveWorkerCount(int? jobWorkerCount, int configuredDefault)
    {
        return Math.Max(1, jobWorkerCount ?? configuredDefault);
    }
}
