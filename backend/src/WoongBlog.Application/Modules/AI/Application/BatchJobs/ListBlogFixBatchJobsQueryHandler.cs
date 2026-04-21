using MediatR;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

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
