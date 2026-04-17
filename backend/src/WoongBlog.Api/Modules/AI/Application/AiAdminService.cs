using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application;

public sealed class AiAdminService : IAiAdminService
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly IBlogAiFixService _aiFixService;
    private readonly AiBatchJobSignal _batchJobSignal;
    private readonly AiOptions _options;

    public AiAdminService(
        WoongBlogDbContext dbContext,
        IBlogAiFixService aiFixService,
        AiBatchJobSignal batchJobSignal,
        IOptions<AiOptions> options)
    {
        _dbContext = dbContext;
        _aiFixService = aiFixService;
        _batchJobSignal = batchJobSignal;
        _options = options.Value;
    }

    public IResult RuntimeConfig()
    {
        var availableProviders = BlogAiFixService.GetAvailableProviders(_options);
        var configuredProvider = NormalizeProvider(_options.Provider);
        var provider = availableProviders.Contains(configuredProvider, StringComparer.OrdinalIgnoreCase)
            ? configuredProvider
            : availableProviders[0];
        return Results.Ok(new AiRuntimeConfigResponse(
            Provider: provider,
            AvailableProviders: availableProviders,
            DefaultModel: provider switch
            {
                "azure" => _options.AzureOpenAiDeployment,
                "codex" => _options.CodexModel,
                _ => _options.OpenAiModel,
            },
            CodexModel: _options.CodexModel,
            CodexReasoningEffort: _options.CodexReasoningEffort,
            AllowedCodexModels: _options.CodexAllowedModels,
            AllowedCodexReasoningEfforts: _options.CodexAllowedReasoningEfforts,
            BatchConcurrency: _options.BatchConcurrency,
            BatchCompletedRetentionDays: _options.BatchCompletedRetentionDays,
            DefaultSystemPrompt: BlogAiFixService.GetDefaultBlogFixPrompt()));
    }

    public async Task<IResult> FixBlogAsync(BlogFixRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required." });
        }

        var result = await _aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.BlogFix,
            Provider: request.Provider,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort,
            CustomPrompt: request.CustomPrompt));
        return Results.Ok(new BlogFixResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    public async Task<IResult> FixBlogBatchAsync(BlogFixBatchRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Blogs.AsQueryable();
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
                var aiResult = await _aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                    Mode: AiFixMode.BlogFix,
                    Provider: request.Provider,
                    CodexModel: request.CodexModel,
                    CodexReasoningEffort: request.CodexReasoningEffort,
                    CustomPrompt: request.CustomPrompt));

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
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new BlogFixBatchResponse(results, request.Apply));
    }

    public async Task<IResult> EnrichWorkAsync(WorkEnrichRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required." });
        }

        var result = await _aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.WorkEnrich,
            Title: request.Title,
            Provider: request.Provider,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort,
            CustomPrompt: request.CustomPrompt));
        return Results.Ok(new WorkEnrichResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    public async Task<IResult> CreateBlogFixBatchJobAsync(BlogFixBatchJobCreateRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Blogs.AsQueryable();
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

        var runtimeProvider = ResolveRequestedProvider(request.Provider);
        var runtimeModel = runtimeProvider == "codex"
            ? ResolveCodexModel(_options, request.CodexModel)
            : runtimeProvider == "azure"
                ? _options.AzureOpenAiDeployment
                : _options.OpenAiModel;
        var runtimeReasoning = runtimeProvider == "codex"
            ? ResolveCodexReasoningEffort(_options, request.CodexReasoningEffort)
            : null;
        var customPrompt = NormalizeCustomPrompt(request.CustomPrompt);
        var selectionKey = BuildSelectionKey(
            request.SelectionMode,
            blogs.Select(blog => blog.Id).ToArray(),
            runtimeModel,
            runtimeReasoning,
            customPrompt,
            request.All,
            request.AutoApply,
            request.WorkerCount);
        var workerCount = NormalizeWorkerCount(request.WorkerCount);

        var existingJob = await _dbContext.AiBatchJobs
            .Where(job =>
                job.TargetType == "blog"
                && job.SelectionKey == selectionKey
                && (job.Status == AiBatchJobStates.Queued || job.Status == AiBatchJobStates.Running))
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingJob is not null)
        {
            var existingItems = await _dbContext.AiBatchJobItems
                .Where(item => item.JobId == existingJob.Id)
                .OrderBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);

            if (existingJob.Status == AiBatchJobStates.Queued)
            {
                _batchJobSignal.Notify();
            }

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
            CustomPrompt = customPrompt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var items = blogs.Select(blog => new AiBatchJobItem
        {
            JobId = job.Id,
            EntityId = blog.Id,
            Title = blog.Title,
            Status = AiBatchJobItemStates.Pending,
        }).ToList();

        _dbContext.AiBatchJobs.Add(job);
        _dbContext.AiBatchJobItems.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _batchJobSignal.Notify();

        return Results.Ok(ToJobDetailResponse(job, items, includeHtml: false));
    }

    public async Task<IResult> ListBlogFixBatchJobsAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog")
            .GroupBy(job => job.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var jobs = await _dbContext.AiBatchJobs
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

    public async Task<IResult> GetBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        var items = await _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToJobDetailResponse(job, items, includeHtml: true));
    }

    public async Task<IResult> ApplyBlogFixBatchJobAsync(
        Guid jobId,
        BlogFixBatchJobApplyRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        var itemsQuery = _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId && item.Status == AiBatchJobItemStates.Succeeded && item.AppliedAt == null);

        if (request.JobItemIds is { Count: > 0 })
        {
            itemsQuery = itemsQuery.Where(item => request.JobItemIds.Contains(item.Id));
        }

        var items = await itemsQuery.ToListAsync(cancellationToken);
        var blogIds = items.Select(item => item.EntityId).ToArray();
        var blogs = await _dbContext.Blogs.Where(blog => blogIds.Contains(blog.Id)).ToDictionaryAsync(blog => blog.Id, cancellationToken);
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

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshJobAggregatesAsync(job, cancellationToken);

        var refreshedItems = await _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToJobDetailResponse(job, refreshedItems, includeHtml: true));
    }

    public async Task<IResult> CancelBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
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
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToJobSummaryResponse(job));
    }

    public async Task<IResult> CancelQueuedBlogFixBatchJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Queued)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return Results.Ok(new { cancelled = 0 });
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await _dbContext.AiBatchJobItems
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

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { cancelled = jobs.Count });
    }

    public async Task<IResult> ClearCompletedBlogFixBatchJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Completed)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return Results.Ok(new { cleared = 0 });
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await _dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId))
            .ToListAsync(cancellationToken);

        _dbContext.AiBatchJobItems.RemoveRange(items);
        _dbContext.AiBatchJobs.RemoveRange(jobs);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { cleared = jobs.Count });
    }

    public async Task<IResult> RemoveBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs
            .SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);

        if (job is null)
        {
            return Results.NotFound();
        }

        if (job.Status is AiBatchJobStates.Queued or AiBatchJobStates.Running)
        {
            return Results.Conflict(new { error = "Only completed, failed, or cancelled jobs can be removed." });
        }

        var items = await _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .ToListAsync(cancellationToken);

        _dbContext.AiBatchJobItems.RemoveRange(items);
        _dbContext.AiBatchJobs.Remove(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { removed = 1, jobId });
    }

    private static string NormalizeProvider(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "azureopenai" => "azure",
            "azure-openai" => "azure",
            "azure" => "azure",
            "codex" => "codex",
            _ => "openai"
        };

    private string ResolveRequestedProvider(string? requestedProvider)
    {
        var normalizedRequested = NormalizeProvider(requestedProvider);
        var availableProviders = BlogAiFixService.GetAvailableProviders(_options);
        if (!string.IsNullOrWhiteSpace(requestedProvider)
            && availableProviders.Contains(normalizedRequested, StringComparer.OrdinalIgnoreCase))
        {
            return normalizedRequested;
        }

        var runtimeProvider = NormalizeProvider(_options.Provider);
        return availableProviders.Contains(runtimeProvider, StringComparer.OrdinalIgnoreCase)
            ? runtimeProvider
            : availableProviders[0];
    }

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
        string? customPrompt,
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
            customPrompt ?? string.Empty,
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

    private static string? NormalizeCustomPrompt(string? customPrompt) =>
        string.IsNullOrWhiteSpace(customPrompt) ? null : customPrompt.Trim();

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
        job.CustomPrompt,
        job.CreatedAt,
        job.StartedAt,
        job.FinishedAt,
        job.CancelRequested);

    private static BlogFixBatchJobDetailResponse ToJobDetailResponse(
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

    private async Task RefreshJobAggregatesAsync(AiBatchJob job, CancellationToken cancellationToken)
    {
        var items = await _dbContext.AiBatchJobItems.Where(item => item.JobId == job.Id).ToListAsync(cancellationToken);
        job.TotalCount = items.Count;
        job.ProcessedCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Failed or AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled);
        job.SucceededCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Applied);
        job.FailedCount = items.Count(item => item.Status == AiBatchJobItemStates.Failed);
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
