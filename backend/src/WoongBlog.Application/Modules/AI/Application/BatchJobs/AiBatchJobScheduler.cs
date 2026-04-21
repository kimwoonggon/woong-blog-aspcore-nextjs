using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class AiBatchJobScheduler(
    IAiBlogFixBatchStore store,
    IAiBatchJobRunner runner) : IAiBatchJobScheduler
{
    public async Task ResetRunningJobsAsync(CancellationToken cancellationToken)
    {
        var runningJobs = await store.GetRunningBlogJobsAsync(cancellationToken);
        if (runningJobs.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var job in runningJobs)
        {
            AiBatchJobProgressPolicy.MarkQueued(job, now);
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessQueuedJobsUntilEmptyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var nextJob = await TryStartNextQueuedJobAsync(cancellationToken);
            if (nextJob is null)
            {
                return;
            }

            await runner.RunAsync(nextJob.Id, cancellationToken);
        }
    }

    private async Task<AiBatchJob?> TryStartNextQueuedJobAsync(CancellationToken cancellationToken)
    {
        var nextJob = (await store.GetQueuedBlogJobsAsync(cancellationToken)).FirstOrDefault();
        if (nextJob is null)
        {
            return null;
        }

        AiBatchJobProgressPolicy.MarkRunning(nextJob, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return nextJob;
    }
}
