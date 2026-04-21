using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class RemoveBlogFixBatchJobCommandHandler(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchJobCommandStore commandStore)
    : IRequestHandler<RemoveBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobRemoveResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobRemoveResponse>> Handle(RemoveBlogFixBatchJobCommand request, CancellationToken cancellationToken)
    {
        var job = await jobQueryStore.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobRemoveResponse>.NotFound();
        }

        if (job.Status is AiBatchJobStates.Queued or AiBatchJobStates.Running)
        {
            return AiActionResult<BlogFixBatchJobRemoveResponse>.Conflict("Only completed, failed, or cancelled jobs can be removed.");
        }

        var items = await jobQueryStore.GetJobItemsAsync(request.JobId, cancellationToken);
        commandStore.RemoveItems(items);
        commandStore.RemoveJobs([job]);
        await commandStore.SaveChangesAsync(cancellationToken);

        return AiActionResult<BlogFixBatchJobRemoveResponse>.Ok(new BlogFixBatchJobRemoveResponse(1, request.JobId));
    }
}
