using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class RemoveBlogFixBatchJobCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<RemoveBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobRemoveResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobRemoveResponse>> Handle(RemoveBlogFixBatchJobCommand request, CancellationToken cancellationToken)
    {
        var job = await store.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobRemoveResponse>.NotFound();
        }

        if (job.Status is AiBatchJobStates.Queued or AiBatchJobStates.Running)
        {
            return AiActionResult<BlogFixBatchJobRemoveResponse>.Conflict("Only completed, failed, or cancelled jobs can be removed.");
        }

        var items = await store.GetJobItemsAsync(request.JobId, cancellationToken);
        store.RemoveItems(items);
        store.RemoveJobs([job]);
        await store.SaveChangesAsync(cancellationToken);

        return AiActionResult<BlogFixBatchJobRemoveResponse>.Ok(new BlogFixBatchJobRemoveResponse(1, request.JobId));
    }
}
