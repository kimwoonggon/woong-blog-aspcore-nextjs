using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Application;

internal static class AiBatchJobResponseMapper
{
    public static BlogFixBatchJobSummaryResponse ToJobSummaryResponse(AiBatchJob job) => new(
        job.Id,
        job.Status,
        job.SelectionMode,
        job.SelectionLabel,
        job.SelectionKey,
        job.AutoApply,
        job.WorkerCount,
        job.TotalCount,
        job.ProcessedCount,
        job.SucceededCount,
        job.FailedCount,
        job.Provider,
        job.Model,
        job.ReasoningEffort,
        job.CustomPrompt,
        job.CreatedAt,
        job.StartedAt,
        job.FinishedAt,
        job.CancelRequested);

    public static BlogFixBatchJobDetailResponse ToJobDetailResponse(
        AiBatchJob job,
        IReadOnlyList<AiBatchJobItem> items,
        bool includeHtml) => new(
        job.Id,
        job.Status,
        job.SelectionMode,
        job.SelectionLabel,
        job.SelectionKey,
        job.AutoApply,
        job.WorkerCount,
        job.TotalCount,
        job.ProcessedCount,
        job.SucceededCount,
        job.FailedCount,
        job.Provider,
        job.Model,
        job.ReasoningEffort,
        job.CustomPrompt,
        job.CreatedAt,
        job.StartedAt,
        job.FinishedAt,
        job.CancelRequested,
        items.Select(item => new BlogFixBatchJobItemResponse(
            item.Id,
            item.EntityId,
            item.Title,
            item.Status,
            includeHtml ? item.FixedHtml : null,
            item.Error,
            item.Provider,
            item.Model,
            item.ReasoningEffort,
            item.AppliedAt)).ToList());
}
