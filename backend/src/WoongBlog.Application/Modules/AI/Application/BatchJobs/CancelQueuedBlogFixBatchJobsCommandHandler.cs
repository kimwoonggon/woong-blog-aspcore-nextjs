using MediatR;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class CancelQueuedBlogFixBatchJobsCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<CancelQueuedBlogFixBatchJobsCommand, BlogFixBatchJobCancelQueuedResponse>
{
    public async Task<BlogFixBatchJobCancelQueuedResponse> Handle(CancelQueuedBlogFixBatchJobsCommand request, CancellationToken cancellationToken)
    {
        var jobs = await store.GetQueuedBlogJobsAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            return new BlogFixBatchJobCancelQueuedResponse(0);
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await store.GetPendingItemsForJobsAsync(jobIds, cancellationToken);
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

        await store.SaveChangesAsync(cancellationToken);
        return new BlogFixBatchJobCancelQueuedResponse(jobs.Count);
    }
}
