using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Domain.Entities;
using Portfolio.Api.Infrastructure.Ai;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Endpoints;

public static class AdminAiEndpoints
{
    public static IEndpointRouteBuilder MapAdminAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ai")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI");

        group.MapGet("/runtime-config", RuntimeConfigAsync)
            .WithName("AdminAiRuntimeConfig")
            .WithSummary("Return the active AI runtime configuration used by admin tools.")
            .WithOpenApi();

        group.MapPost("/blog-fix", FixBlogAsync)
            .WithName("AdminAiFixBlog")
            .WithSummary("Fix a single blog HTML payload with the configured AI provider.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch", FixBlogBatchAsync)
            .WithName("AdminAiFixBlogBatch")
            .WithSummary("Fix one or more saved blogs, optionally applying the results.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs", CreateBlogFixBatchJobAsync)
            .WithName("AdminAiCreateBlogFixBatchJob")
            .WithSummary("Create an asynchronous blog AI fix batch job.")
            .WithOpenApi();

        group.MapGet("/blog-fix-batch-jobs", ListBlogFixBatchJobsAsync)
            .WithName("AdminAiListBlogFixBatchJobs")
            .WithSummary("List recent blog AI fix batch jobs.")
            .WithOpenApi();

        group.MapGet("/blog-fix-batch-jobs/{jobId:guid}", GetBlogFixBatchJobAsync)
            .WithName("AdminAiGetBlogFixBatchJob")
            .WithSummary("Get blog AI fix batch job detail and per-item results.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/{jobId:guid}/apply", ApplyBlogFixBatchJobAsync)
            .WithName("AdminAiApplyBlogFixBatchJob")
            .WithSummary("Apply successful results from a completed blog AI fix batch job.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/{jobId:guid}/cancel", CancelBlogFixBatchJobAsync)
            .WithName("AdminAiCancelBlogFixBatchJob")
            .WithSummary("Request cancellation for a queued or running blog AI fix batch job.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/cancel-queued", CancelQueuedBlogFixBatchJobsAsync)
            .WithName("AdminAiCancelQueuedBlogFixBatchJobs")
            .WithSummary("Cancel all queued blog AI fix batch jobs.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch-jobs/clear-completed", ClearCompletedBlogFixBatchJobsAsync)
            .WithName("AdminAiClearCompletedBlogFixBatchJobs")
            .WithSummary("Delete completed blog AI fix batch jobs and their item history.")
            .WithOpenApi();

        group.MapDelete("/blog-fix-batch-jobs/{jobId:guid}", RemoveBlogFixBatchJobAsync)
            .WithName("AdminAiRemoveBlogFixBatchJob")
            .WithSummary("Delete a completed, failed, or cancelled blog AI fix batch job and its item history.")
            .WithOpenApi();

        group.MapPost("/work-enrich", EnrichWorkAsync)
            .WithName("AdminAiEnrichWork")
            .WithSummary("Enrich a single work HTML payload with the configured AI provider.")
            .WithOpenApi();

        return app;
    }

    private static IResult RuntimeConfigAsync(IOptions<AiOptions> options)
    {
        var config = options.Value;
        return Results.Ok(new AiRuntimeConfigResponse(
            Provider: NormalizeProvider(config.Provider),
            DefaultModel: NormalizeProvider(config.Provider) switch
            {
                "azure" => config.AzureOpenAiDeployment,
                "codex" => config.CodexModel,
                _ => config.OpenAiModel,
            },
            CodexModel: config.CodexModel,
            CodexReasoningEffort: config.CodexReasoningEffort,
            AllowedCodexModels: config.CodexAllowedModels,
            AllowedCodexReasoningEfforts: config.CodexAllowedReasoningEfforts,
            BatchConcurrency: config.BatchConcurrency,
            BatchCompletedRetentionDays: config.BatchCompletedRetentionDays));
    }

    private static async Task<IResult> FixBlogAsync(
        BlogFixRequest request,
        IBlogAiFixService aiFixService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required." });
        }

        var result = await aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.BlogFix,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort));
        return Results.Ok(new BlogFixResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    private static async Task<IResult> FixBlogBatchAsync(
        BlogFixBatchRequest request,
        PortfolioDbContext dbContext,
        IBlogAiFixService aiFixService,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Blogs.AsQueryable();
        if (!request.All)
        {
            if (request.BlogIds is null || request.BlogIds.Count == 0)
            {
                return Results.BadRequest(new { error = "Either blogIds or all=true is required." });
            }

            query = query.Where(blog => request.BlogIds.Contains(blog.Id));
        }

        var blogs = await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .ToListAsync(cancellationToken);

        var results = new List<BlogFixBatchItemResponse>(blogs.Count);
        foreach (var blog in blogs)
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);

            try
            {
                var aiResult = await aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                    Mode: AiFixMode.BlogFix,
                    CodexModel: request.CodexModel,
                    CodexReasoningEffort: request.CodexReasoningEffort));

                if (request.Apply)
                {
                    blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(aiResult.FixedHtml)}}}""";
                    blog.Excerpt = AdminContentText.GenerateExcerpt(aiResult.FixedHtml);
                    blog.UpdatedAt = DateTimeOffset.UtcNow;
                }

                results.Add(new BlogFixBatchItemResponse(
                    blog.Id,
                    blog.Title,
                    "fixed",
                    aiResult.FixedHtml,
                    null,
                    aiResult.Provider,
                    aiResult.Model,
                    aiResult.ReasoningEffort));
            }
            catch (Exception exception)
            {
                results.Add(new BlogFixBatchItemResponse(
                    blog.Id,
                    blog.Title,
                    "failed",
                    null,
                    exception.Message,
                    null,
                    null,
                    null));
            }
        }

        if (request.Apply)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new BlogFixBatchResponse(results, request.Apply));
    }

    private static async Task<IResult> EnrichWorkAsync(
        WorkEnrichRequest request,
        IBlogAiFixService aiFixService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required." });
        }

        var result = await aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.WorkEnrich,
            Title: request.Title,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort));
        return Results.Ok(new WorkEnrichResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    private static async Task<IResult> CreateBlogFixBatchJobAsync(
        BlogFixBatchJobCreateRequest request,
        PortfolioDbContext dbContext,
        IOptions<AiOptions> options,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Blogs.AsQueryable();
        if (!request.All)
        {
            if (request.BlogIds is null || request.BlogIds.Count == 0)
            {
                return Results.BadRequest(new { error = "Either blogIds or all=true is required." });
            }

            query = query.Where(blog => request.BlogIds.Contains(blog.Id));
        }

        var blogs = await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .Select(blog => new { blog.Id, blog.Title })
            .ToListAsync(cancellationToken);

        if (blogs.Count == 0)
        {
            return Results.BadRequest(new { error = "No matching blogs were found." });
        }

        var runtimeProvider = NormalizeProvider(options.Value.Provider);
        var runtimeModel = runtimeProvider == "codex"
            ? ResolveCodexModel(options.Value, request.CodexModel)
            : runtimeProvider == "azure"
                ? options.Value.AzureOpenAiDeployment
                : options.Value.OpenAiModel;
        var runtimeReasoning = runtimeProvider == "codex"
            ? ResolveCodexReasoningEffort(options.Value, request.CodexReasoningEffort)
            : null;
        var selectionKey = BuildSelectionKey(
            request.SelectionMode,
            blogs.Select(blog => blog.Id).ToArray(),
            runtimeModel,
            runtimeReasoning,
            request.All,
            request.AutoApply,
            request.WorkerCount);
        var workerCount = NormalizeWorkerCount(request.WorkerCount);

        var existingJob = await dbContext.AiBatchJobs
            .Where(job =>
                job.TargetType == "blog"
                && job.SelectionKey == selectionKey
                && (job.Status == AiBatchJobStates.Queued || job.Status == AiBatchJobStates.Running))
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingJob is not null)
        {
            var existingItems = await dbContext.AiBatchJobItems
                .Where(item => item.JobId == existingJob.Id)
                .OrderBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);

            return Results.Ok(ToJobDetailResponse(existingJob, existingItems, includeHtml: false));
        }

        var job = new AiBatchJob
        {
            TargetType = "blog",
            Status = AiBatchJobStates.Queued,
            SelectionMode = request.SelectionMode ?? "selected",
            SelectionLabel = request.SelectionLabel ?? string.Empty,
            SelectionKey = selectionKey,
            All = request.All,
            AutoApply = request.AutoApply,
            WorkerCount = workerCount,
            TotalCount = blogs.Count,
            Provider = runtimeProvider,
            Model = runtimeModel,
            ReasoningEffort = runtimeReasoning,
            PromptMode = "blog-fix",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var items = blogs.Select(blog => new AiBatchJobItem
        {
            JobId = job.Id,
            EntityId = blog.Id,
            Title = blog.Title,
            Status = AiBatchJobItemStates.Pending,
        }).ToList();

        dbContext.AiBatchJobs.Add(job);
        dbContext.AiBatchJobItems.AddRange(items);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToJobDetailResponse(job, items, includeHtml: false));
    }

    private static async Task<IResult> ListBlogFixBatchJobsAsync(
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var counts = await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog")
            .GroupBy(job => job.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var jobs = await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog")
            .OrderByDescending(job => job.CreatedAt)
            .Take(20)
            .Select(job => ToJobSummaryResponse(job))
            .ToListAsync(cancellationToken);

        return Results.Ok(new BlogFixBatchJobListResponse(
            jobs,
            RunningCount: counts.Where(x => x.Status == AiBatchJobStates.Running).Select(x => x.Count).SingleOrDefault(),
            QueuedCount: counts.Where(x => x.Status == AiBatchJobStates.Queued).Select(x => x.Count).SingleOrDefault(),
            CompletedCount: counts.Where(x => x.Status == AiBatchJobStates.Completed).Select(x => x.Count).SingleOrDefault(),
            FailedCount: counts.Where(x => x.Status == AiBatchJobStates.Failed).Select(x => x.Count).SingleOrDefault(),
            CancelledCount: counts.Where(x => x.Status == AiBatchJobStates.Cancelled).Select(x => x.Count).SingleOrDefault()));
    }

    private static async Task<IResult> GetBlogFixBatchJobAsync(
        Guid jobId,
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        var items = await dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToJobDetailResponse(job, items, includeHtml: true));
    }

    private static async Task<IResult> ApplyBlogFixBatchJobAsync(
        Guid jobId,
        BlogFixBatchJobApplyRequest request,
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        var itemsQuery = dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId && item.Status == AiBatchJobItemStates.Succeeded && item.AppliedAt == null);

        if (request.JobItemIds is { Count: > 0 })
        {
            itemsQuery = itemsQuery.Where(item => request.JobItemIds.Contains(item.Id));
        }

        var items = await itemsQuery.ToListAsync(cancellationToken);
        var blogIds = items.Select(item => item.EntityId).ToArray();
        var blogs = await dbContext.Blogs.Where(blog => blogIds.Contains(blog.Id)).ToDictionaryAsync(blog => blog.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var item in items)
        {
            if (!blogs.TryGetValue(item.EntityId, out var blog) || string.IsNullOrWhiteSpace(item.FixedHtml))
            {
                continue;
            }

            blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(item.FixedHtml)}}}""";
            blog.Excerpt = AdminContentText.GenerateExcerpt(item.FixedHtml);
            blog.UpdatedAt = now;
            item.AppliedAt = now;
            item.Status = AiBatchJobItemStates.Applied;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RefreshJobAggregatesAsync(dbContext, job, cancellationToken);

        var refreshedItems = await dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToJobDetailResponse(job, refreshedItems, includeHtml: true));
    }

    private static async Task<IResult> CancelBlogFixBatchJobAsync(
        Guid jobId,
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        if (job.Status is AiBatchJobStates.Completed or AiBatchJobStates.Failed or AiBatchJobStates.Cancelled)
        {
            return Results.Ok(ToJobSummaryResponse(job));
        }

        job.CancelRequested = true;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToJobSummaryResponse(job));
    }

    private static async Task<IResult> CancelQueuedBlogFixBatchJobsAsync(
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var jobs = await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Queued)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return Results.Ok(new { cancelled = 0 });
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId) && item.Status == AiBatchJobItemStates.Pending)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            job.CancelRequested = true;
            job.Status = AiBatchJobStates.Cancelled;
            job.FinishedAt = now;
            job.UpdatedAt = now;
        }

        foreach (var item in items)
        {
            item.Status = AiBatchJobItemStates.Cancelled;
            item.FinishedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { cancelled = jobs.Count });
    }

    private static async Task<IResult> ClearCompletedBlogFixBatchJobsAsync(
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var jobs = await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Completed)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return Results.Ok(new { cleared = 0 });
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId))
            .ToListAsync(cancellationToken);

        dbContext.AiBatchJobItems.RemoveRange(items);
        dbContext.AiBatchJobs.RemoveRange(jobs);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { cleared = jobs.Count });
    }

    private static async Task<IResult> RemoveBlogFixBatchJobAsync(
        Guid jobId,
        PortfolioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.AiBatchJobs
            .SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);

        if (job is null)
        {
            return Results.NotFound();
        }

        if (job.Status is AiBatchJobStates.Queued or AiBatchJobStates.Running)
        {
            return Results.Conflict(new { error = "Only completed, failed, or cancelled jobs can be removed." });
        }

        var items = await dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .ToListAsync(cancellationToken);

        dbContext.AiBatchJobItems.RemoveRange(items);
        dbContext.AiBatchJobs.Remove(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { removed = 1, jobId });
    }

    public sealed record BlogFixRequest(string Html, string? CodexModel = null, string? CodexReasoningEffort = null);

    public sealed record BlogFixResponse(string FixedHtml, string Provider, string Model, string? ReasoningEffort);

    public sealed record BlogFixBatchRequest(
        IReadOnlyList<Guid>? BlogIds,
        bool All,
        bool Apply,
        string? CodexModel = null,
        string? CodexReasoningEffort = null);

    public sealed record BlogFixBatchItemResponse(
        Guid BlogId,
        string Title,
        string Status,
        string? FixedHtml,
        string? Error,
        string? Provider,
        string? Model,
        string? ReasoningEffort);

    public sealed record BlogFixBatchResponse(
        IReadOnlyList<BlogFixBatchItemResponse> Results,
        bool Applied);

    public sealed record WorkEnrichRequest(
        string Html,
        string? Title = null,
        string? CodexModel = null,
        string? CodexReasoningEffort = null);

    public sealed record WorkEnrichResponse(
        string FixedHtml,
        string Provider,
        string Model,
        string? ReasoningEffort);

    public sealed record AiRuntimeConfigResponse(
        string Provider,
        string DefaultModel,
        string CodexModel,
        string CodexReasoningEffort,
        IReadOnlyList<string> AllowedCodexModels,
        IReadOnlyList<string> AllowedCodexReasoningEfforts,
        int BatchConcurrency,
        int BatchCompletedRetentionDays);

    public sealed record BlogFixBatchJobCreateRequest(
        IReadOnlyList<Guid>? BlogIds,
        bool All,
        string? SelectionMode = null,
        string? SelectionLabel = null,
        string? SelectionKey = null,
        bool AutoApply = false,
        int? WorkerCount = null,
        string? CodexModel = null,
        string? CodexReasoningEffort = null);

    public sealed record BlogFixBatchJobApplyRequest(
        IReadOnlyList<Guid>? JobItemIds = null);

    public sealed record BlogFixBatchJobListResponse(
        IReadOnlyList<BlogFixBatchJobSummaryResponse> Jobs,
        int RunningCount,
        int QueuedCount,
        int CompletedCount,
        int FailedCount,
        int CancelledCount);

    public sealed record BlogFixBatchJobSummaryResponse(
        Guid JobId,
        string Status,
        string SelectionMode,
        string SelectionLabel,
        string SelectionKey,
        bool AutoApply,
        int? WorkerCount,
        int TotalCount,
        int ProcessedCount,
        int SucceededCount,
        int FailedCount,
        string Provider,
        string Model,
        string? ReasoningEffort,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        bool CancelRequested);

    public sealed record BlogFixBatchJobItemResponse(
        Guid JobItemId,
        Guid BlogId,
        string Title,
        string Status,
        string? FixedHtml,
        string? Error,
        string? Provider,
        string? Model,
        string? ReasoningEffort,
        DateTimeOffset? AppliedAt);

    public sealed record BlogFixBatchJobDetailResponse(
        Guid JobId,
        string Status,
        string SelectionMode,
        string SelectionLabel,
        string SelectionKey,
        bool AutoApply,
        int? WorkerCount,
        int TotalCount,
        int ProcessedCount,
        int SucceededCount,
        int FailedCount,
        string Provider,
        string Model,
        string? ReasoningEffort,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        bool CancelRequested,
        IReadOnlyList<BlogFixBatchJobItemResponse> Items);

    private static string NormalizeProvider(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "azureopenai" => "azure",
            "azure-openai" => "azure",
            "azure" => "azure",
            "codex" => "codex",
            _ => "openai"
        };

    private static string ResolveCodexModel(AiOptions options, string? overrideValue)
    {
        var candidate = string.IsNullOrWhiteSpace(overrideValue) ? options.CodexModel : overrideValue.Trim();
        return options.CodexAllowedModels.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            ? candidate
            : options.CodexModel;
    }

    private static string ResolveCodexReasoningEffort(AiOptions options, string? overrideValue)
    {
        var candidate = string.IsNullOrWhiteSpace(overrideValue) ? options.CodexReasoningEffort : overrideValue.Trim().ToLowerInvariant();
        return options.CodexAllowedReasoningEfforts.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            ? candidate
            : options.CodexReasoningEffort;
    }

    private static string BuildSelectionKey(
        string? selectionMode,
        IReadOnlyList<Guid> blogIds,
        string runtimeModel,
        string? runtimeReasoning,
        bool all,
        bool autoApply,
        int? workerCount)
    {
        var canonical = string.Join(
            '|',
            selectionMode ?? "selected",
            all ? "all" : "subset",
            runtimeModel,
            runtimeReasoning ?? string.Empty,
            autoApply ? "auto-apply" : "manual-apply",
            NormalizeWorkerCount(workerCount)?.ToString() ?? "default-workers",
            string.Join(",", blogIds.OrderBy(id => id)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static int? NormalizeWorkerCount(int? workerCount)
    {
        if (workerCount is null)
        {
            return null;
        }

        return Math.Clamp(workerCount.Value, 1, 8);
    }

    private static BlogFixBatchJobSummaryResponse ToJobSummaryResponse(AiBatchJob job) => new(
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
        job.CreatedAt,
        job.StartedAt,
        job.FinishedAt,
        job.CancelRequested);

    private static BlogFixBatchJobDetailResponse ToJobDetailResponse(AiBatchJob job, IReadOnlyList<AiBatchJobItem> items, bool includeHtml) => new(
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

    private static async Task RefreshJobAggregatesAsync(PortfolioDbContext dbContext, AiBatchJob job, CancellationToken cancellationToken)
    {
        var items = await dbContext.AiBatchJobItems.Where(item => item.JobId == job.Id).ToListAsync(cancellationToken);
        job.TotalCount = items.Count;
        job.ProcessedCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Failed or AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled);
        job.SucceededCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Applied);
        job.FailedCount = items.Count(item => item.Status == AiBatchJobItemStates.Failed);
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
