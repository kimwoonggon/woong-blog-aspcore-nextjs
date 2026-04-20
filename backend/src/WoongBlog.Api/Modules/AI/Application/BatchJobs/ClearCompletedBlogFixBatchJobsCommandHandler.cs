using MediatR;
using WoongBlog.Api.Modules.AI.Api;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class ClearCompletedBlogFixBatchJobsCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<ClearCompletedBlogFixBatchJobsCommand, BlogFixBatchJobClearCompletedResponse>
{
    public async Task<BlogFixBatchJobClearCompletedResponse> Handle(ClearCompletedBlogFixBatchJobsCommand request, CancellationToken cancellationToken)
    {
        var jobs = await store.GetCompletedBlogJobsAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            return new BlogFixBatchJobClearCompletedResponse(0);
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await store.GetItemsForJobsAsync(jobIds, cancellationToken);

        store.RemoveItems(items);
        store.RemoveJobs(jobs);
        await store.SaveChangesAsync(cancellationToken);

        return new BlogFixBatchJobClearCompletedResponse(jobs.Count);
    }
}
