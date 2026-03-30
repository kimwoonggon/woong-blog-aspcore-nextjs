namespace WoongBlog.Api.Endpoints;

public static class AdminAiEndpoints
{
    public static IEndpointRouteBuilder MapAdminAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ai")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI");

        group.MapGet("/runtime-config", (IAdminAiWorkflowService workflowService) => Results.Ok(workflowService.RuntimeConfig()))
            .WithName("AdminAiRuntimeConfig")
            .WithSummary("Return the active AI runtime configuration used by admin tools.")
            .WithOpenApi();

        group.MapPost("/blog-fix", async (BlogFixRequest request, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.FixBlogAsync(request, cancellationToken)))
            .WithName("AdminAiFixBlog")
            .WithSummary("Fix a single blog HTML payload with the configured AI provider.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch", async (BlogFixBatchRequest request, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.FixBlogBatchAsync(request, cancellationToken)))
            .WithName("AdminAiFixBlogBatch")
            .WithSummary("Fix one or more saved blogs, optionally applying the results.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs", async (BlogFixBatchJobCreateRequest request, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.CreateBlogFixBatchJobAsync(request, cancellationToken)))
            .WithName("AdminAiCreateBlogFixBatchJob")
            .WithSummary("Create an asynchronous blog AI fix batch job.")
            .WithOpenApi();

        group.MapGet("/blog-fix-batch-jobs", async (IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.ListBlogFixBatchJobsAsync(cancellationToken)))
            .WithName("AdminAiListBlogFixBatchJobs")
            .WithSummary("List recent blog AI fix batch jobs.")
            .WithOpenApi();

        group.MapGet("/blog-fix-batch-jobs/{jobId:guid}", async (Guid jobId, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.GetBlogFixBatchJobAsync(jobId, cancellationToken)))
            .WithName("AdminAiGetBlogFixBatchJob")
            .WithSummary("Get blog AI fix batch job detail and per-item results.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/{jobId:guid}/apply", async (Guid jobId, BlogFixBatchJobApplyRequest request, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.ApplyBlogFixBatchJobAsync(jobId, request, cancellationToken)))
            .WithName("AdminAiApplyBlogFixBatchJob")
            .WithSummary("Apply successful results from a completed blog AI fix batch job.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/{jobId:guid}/cancel", async (Guid jobId, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.CancelBlogFixBatchJobAsync(jobId, cancellationToken)))
            .WithName("AdminAiCancelBlogFixBatchJob")
            .WithSummary("Request cancellation for a queued or running blog AI fix batch job.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/cancel-queued", async (IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.CancelQueuedBlogFixBatchJobsAsync(cancellationToken), "cancelled"))
            .WithName("AdminAiCancelQueuedBlogFixBatchJobs")
            .WithSummary("Cancel all queued blog AI fix batch jobs.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/clear-completed", async (IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.ClearCompletedBlogFixBatchJobsAsync(cancellationToken), "cleared"))
            .WithName("AdminAiClearCompletedBlogFixBatchJobs")
            .WithSummary("Delete completed blog AI fix batch jobs and their item history.")
            .WithOpenApi();

        group.MapDelete("/blog-fix-batch-jobs/{jobId:guid}", async (Guid jobId, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.RemoveBlogFixBatchJobAsync(jobId, cancellationToken)))
            .WithName("AdminAiRemoveBlogFixBatchJob")
            .WithSummary("Delete a completed, failed, or cancelled blog AI fix batch job and its item history.")
            .WithOpenApi();

        group.MapPost("/work-enrich", async (WorkEnrichRequest request, IAdminAiWorkflowService workflowService, CancellationToken cancellationToken) =>
                ToResult(await workflowService.EnrichWorkAsync(request, cancellationToken)))
            .WithName("AdminAiEnrichWork")
            .WithSummary("Enrich a single work HTML payload with the configured AI provider.")
            .WithOpenApi();

        return app;
    }

    private static IResult ToResult<T>(AdminAiOperationResult<T> result)
        where T : class
    {
        if (result.Failure is null)
        {
            return Results.Ok(result.Value);
        }

        return result.Failure.Kind switch
        {
            AdminAiFailureKind.Validation => Results.BadRequest(new { error = result.Failure.Message }),
            AdminAiFailureKind.NotFound => Results.NotFound(),
            AdminAiFailureKind.Conflict => Results.Conflict(new { error = result.Failure.Message }),
            AdminAiFailureKind.System => Results.Json(new { error = result.Failure.Message }, statusCode: StatusCodes.Status500InternalServerError),
            _ => Results.BadRequest(new { error = result.Failure.Message })
        };
    }

    private static IResult ToResult(AdminAiOperationResult<AdminAiCountResponse> result, string propertyName)
    {
        if (result.Failure is null)
        {
            return propertyName switch
            {
                "cancelled" => Results.Ok(new { cancelled = result.Value!.Count }),
                "cleared" => Results.Ok(new { cleared = result.Value!.Count }),
                _ => Results.Ok(new { count = result.Value!.Count })
            };
        }

        return ToResult<object>(new AdminAiOperationResult<object>(null, result.Failure));
    }

    public sealed record BlogFixRequest(string Html, string? CodexModel = null, string? CodexReasoningEffort = null);
    public sealed record BlogFixResponse(string FixedHtml, string Provider, string Model, string? ReasoningEffort);
    public sealed record BlogFixBatchRequest(IReadOnlyList<Guid>? BlogIds, bool All, bool Apply, string? CodexModel = null, string? CodexReasoningEffort = null);
    public sealed record BlogFixBatchItemResponse(Guid BlogId, string Title, string Status, string? FixedHtml, string? Error, string? Provider, string? Model, string? ReasoningEffort);
    public sealed record BlogFixBatchResponse(IReadOnlyList<BlogFixBatchItemResponse> Results, bool Applied);
    public sealed record WorkEnrichRequest(string Html, string? Title = null, string? CodexModel = null, string? CodexReasoningEffort = null);
    public sealed record WorkEnrichResponse(string FixedHtml, string Provider, string Model, string? ReasoningEffort);
    public sealed record AiRuntimeConfigResponse(string Provider, string DefaultModel, string CodexModel, string CodexReasoningEffort, IReadOnlyList<string> AllowedCodexModels, IReadOnlyList<string> AllowedCodexReasoningEfforts, int BatchConcurrency, int BatchCompletedRetentionDays);
    public sealed record BlogFixBatchJobCreateRequest(IReadOnlyList<Guid>? BlogIds, bool All, string? SelectionMode = null, string? SelectionLabel = null, string? SelectionKey = null, bool AutoApply = false, int? WorkerCount = null, string? CodexModel = null, string? CodexReasoningEffort = null);
    public sealed record BlogFixBatchJobApplyRequest(IReadOnlyList<Guid>? JobItemIds = null);
    public sealed record BlogFixBatchJobListResponse(IReadOnlyList<BlogFixBatchJobSummaryResponse> Jobs, int RunningCount, int QueuedCount, int CompletedCount, int FailedCount, int CancelledCount);
    public sealed record BlogFixBatchJobSummaryResponse(Guid JobId, string Status, string SelectionMode, string SelectionLabel, string SelectionKey, bool AutoApply, int? WorkerCount, int TotalCount, int ProcessedCount, int SucceededCount, int FailedCount, string Provider, string Model, string? ReasoningEffort, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, bool CancelRequested);
    public sealed record BlogFixBatchJobItemResponse(Guid JobItemId, Guid BlogId, string Title, string Status, string? FixedHtml, string? Error, string? Provider, string? Model, string? ReasoningEffort, DateTimeOffset? AppliedAt);
    public sealed record BlogFixBatchJobDetailResponse(Guid JobId, string Status, string SelectionMode, string SelectionLabel, string SelectionKey, bool AutoApply, int? WorkerCount, int TotalCount, int ProcessedCount, int SucceededCount, int FailedCount, string Provider, string Model, string? ReasoningEffort, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, bool CancelRequested, IReadOnlyList<BlogFixBatchJobItemResponse> Items);
}
