using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Api;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class CancelBlogFixBatchJobCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<CancelBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobSummaryResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobSummaryResponse>> Handle(CancelBlogFixBatchJobCommand request, CancellationToken cancellationToken)
    {
        var job = await store.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobSummaryResponse>.NotFound();
        }

        if (job.Status is not (AiBatchJobStates.Completed or AiBatchJobStates.Failed or AiBatchJobStates.Cancelled))
        {
            job.CancelRequested = true;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await store.SaveChangesAsync(cancellationToken);
        }

        return AiActionResult<BlogFixBatchJobSummaryResponse>.Ok(
            AiBatchJobResponseMapper.ToJobSummaryResponse(job));
    }
}
