using MediatR;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class ClearCompletedBlogFixBatchJobsCommandHandler(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchJobCommandStore commandStore)
    : IRequestHandler<ClearCompletedBlogFixBatchJobsCommand, BlogFixBatchJobClearCompletedResponse>
{
    public async Task<BlogFixBatchJobClearCompletedResponse> Handle(ClearCompletedBlogFixBatchJobsCommand request, CancellationToken cancellationToken)
    {
        var jobs = await jobQueryStore.GetCompletedBlogJobsAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            return new BlogFixBatchJobClearCompletedResponse(0);
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await jobQueryStore.GetItemsForJobsAsync(jobIds, cancellationToken);

        commandStore.RemoveItems(items);
        commandStore.RemoveJobs(jobs);
        await commandStore.SaveChangesAsync(cancellationToken);

        return new BlogFixBatchJobClearCompletedResponse(jobs.Count);
    }
}
