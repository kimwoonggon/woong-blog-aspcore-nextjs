using MediatR;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed record CreateBlogFixBatchJobCommand(
    IReadOnlyList<Guid>? BlogIds,
    bool All,
    string? SelectionMode,
    string? SelectionLabel,
    bool AutoApply,
    int? WorkerCount,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<BlogFixBatchJobDetailResponse>>;

public sealed record ListBlogFixBatchJobsQuery : IRequest<BlogFixBatchJobListResponse>;

public sealed record GetBlogFixBatchJobQuery(Guid JobId) : IRequest<BlogFixBatchJobDetailResponse?>;

public sealed record ApplyBlogFixBatchJobCommand(
    Guid JobId,
    IReadOnlyList<Guid>? JobItemIds) : IRequest<AiActionResult<BlogFixBatchJobDetailResponse>>;

public sealed record CancelBlogFixBatchJobCommand(Guid JobId) : IRequest<AiActionResult<BlogFixBatchJobSummaryResponse>>;

public sealed record CancelQueuedBlogFixBatchJobsCommand : IRequest<BlogFixBatchJobCancelQueuedResponse>;

public sealed record ClearCompletedBlogFixBatchJobsCommand : IRequest<BlogFixBatchJobClearCompletedResponse>;

public sealed record RemoveBlogFixBatchJobCommand(Guid JobId) : IRequest<AiActionResult<BlogFixBatchJobRemoveResponse>>;
