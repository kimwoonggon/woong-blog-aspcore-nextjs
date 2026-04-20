using MediatR;
using WoongBlog.Api.Modules.AI.Api;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class ListBlogFixBatchJobsQueryHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<ListBlogFixBatchJobsQuery, BlogFixBatchJobListResponse>
{
    public async Task<BlogFixBatchJobListResponse> Handle(ListBlogFixBatchJobsQuery request, CancellationToken cancellationToken)
    {
        var counts = await store.GetBlogJobCountsAsync(cancellationToken);
        var jobs = await store.GetRecentBlogJobsAsync(take: 20, cancellationToken);

        return new BlogFixBatchJobListResponse(
            jobs.Select(AiBatchJobResponseMapper.ToJobSummaryResponse).ToList(),
            counts.RunningCount,
            counts.QueuedCount,
            counts.CompletedCount,
            counts.FailedCount,
            counts.CancelledCount);
    }
}
