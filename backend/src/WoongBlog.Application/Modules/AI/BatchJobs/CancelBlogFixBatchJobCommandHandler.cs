using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

public sealed class CancelBlogFixBatchJobCommandHandler(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchJobCommandStore commandStore)
    : IRequestHandler<CancelBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobSummaryResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobSummaryResponse>> Handle(CancelBlogFixBatchJobCommand request, CancellationToken cancellationToken)
    {
        var job = await jobQueryStore.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobSummaryResponse>.NotFound();
        }

        if (job.Status is not (AiBatchJobStates.Completed or AiBatchJobStates.Failed or AiBatchJobStates.Cancelled))
        {
            job.CancelRequested = true;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await commandStore.SaveChangesAsync(cancellationToken);
        }

        return AiActionResult<BlogFixBatchJobSummaryResponse>.Ok(
            AiBatchJobResponseMapper.ToJobSummaryResponse(job));
    }
}
