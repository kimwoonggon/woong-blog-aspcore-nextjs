using Microsoft.AspNetCore.Http;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application;

public interface IAiAdminService
{
    IResult RuntimeConfig();
    Task<IResult> FixBlogAsync(BlogFixRequest request, CancellationToken cancellationToken);
    Task<IResult> FixBlogBatchAsync(BlogFixBatchRequest request, CancellationToken cancellationToken);
    Task<IResult> EnrichWorkAsync(WorkEnrichRequest request, CancellationToken cancellationToken);
    Task<IResult> CreateBlogFixBatchJobAsync(BlogFixBatchJobCreateRequest request, CancellationToken cancellationToken);
    Task<IResult> ListBlogFixBatchJobsAsync(CancellationToken cancellationToken);
    Task<IResult> GetBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IResult> ApplyBlogFixBatchJobAsync(Guid jobId, BlogFixBatchJobApplyRequest request, CancellationToken cancellationToken);
    Task<IResult> CancelBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IResult> CancelQueuedBlogFixBatchJobsAsync(CancellationToken cancellationToken);
    Task<IResult> ClearCompletedBlogFixBatchJobsAsync(CancellationToken cancellationToken);
    Task<IResult> RemoveBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken);
}
