using MediatR;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

public sealed class CancelQueuedBlogFixBatchJobsCommandHandler(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchJobCommandStore commandStore)
    : IRequestHandler<CancelQueuedBlogFixBatchJobsCommand, BlogFixBatchJobCancelQueuedResponse>
{
    public async Task<BlogFixBatchJobCancelQueuedResponse> Handle(CancelQueuedBlogFixBatchJobsCommand request, CancellationToken cancellationToken)
    {
        var jobs = await jobQueryStore.GetQueuedBlogJobsAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            return new BlogFixBatchJobCancelQueuedResponse(0);
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await jobQueryStore.GetPendingItemsForJobsAsync(jobIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            job.CancelRequested = true;
            AiBatchJobProgressPolicy.MarkCancelled(job, now);
        }

        foreach (var item in items)
        {
            AiBatchJobProgressPolicy.MarkCancelled(item, now);
        }

        await commandStore.SaveChangesAsync(cancellationToken);
        return new BlogFixBatchJobCancelQueuedResponse(jobs.Count);
    }
}
