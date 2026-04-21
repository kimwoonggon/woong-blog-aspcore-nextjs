namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public static class AiBatchWorkerPolicy
{
    public static int ResolveWorkerCount(int? jobWorkerCount, int configuredDefault)
    {
        return Math.Max(1, jobWorkerCount ?? configuredDefault);
    }
}
