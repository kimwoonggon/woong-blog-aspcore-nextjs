using MediatR;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

public sealed class ListBlogFixBatchJobsQueryHandler(IAiBatchJobQueryStore jobQueryStore)
    : IRequestHandler<ListBlogFixBatchJobsQuery, BlogFixBatchJobListResponse>
{
    public async Task<BlogFixBatchJobListResponse> Handle(ListBlogFixBatchJobsQuery request, CancellationToken cancellationToken)
    {
        var counts = await jobQueryStore.GetBlogJobCountsAsync(cancellationToken);
        var jobs = await jobQueryStore.GetRecentBlogJobsAsync(take: 20, cancellationToken);

        return new BlogFixBatchJobListResponse(
            jobs.Select(AiBatchJobResponseMapper.ToJobSummaryResponse).ToList(),
            counts.RunningCount,
            counts.QueuedCount,
            counts.CompletedCount,
            counts.FailedCount,
            counts.CancelledCount);
    }
}
