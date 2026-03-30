using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Endpoints;

public interface IAdminAiWorkflowService
{
    AdminAiEndpoints.AiRuntimeConfigResponse RuntimeConfig();
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixResponse>> FixBlogAsync(AdminAiEndpoints.BlogFixRequest request, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchResponse>> FixBlogBatchAsync(AdminAiEndpoints.BlogFixBatchRequest request, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.WorkEnrichResponse>> EnrichWorkAsync(AdminAiEndpoints.WorkEnrichRequest request, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>> CreateBlogFixBatchJobAsync(AdminAiEndpoints.BlogFixBatchJobCreateRequest request, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobListResponse>> ListBlogFixBatchJobsAsync(CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>> GetBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>> ApplyBlogFixBatchJobAsync(Guid jobId, AdminAiEndpoints.BlogFixBatchJobApplyRequest request, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobSummaryResponse>> CancelBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiCountResponse>> CancelQueuedBlogFixBatchJobsAsync(CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiCountResponse>> ClearCompletedBlogFixBatchJobsAsync(CancellationToken cancellationToken);
    Task<AdminAiOperationResult<AdminAiRemoveResponse>> RemoveBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken);
}

public enum AdminAiFailureKind
{
    Validation,
    NotFound,
    Conflict
}

public sealed record AdminAiFailure(AdminAiFailureKind Kind, string Message);

public sealed record AdminAiOperationResult<T>(T? Value, AdminAiFailure? Failure = null)
    where T : class;

public sealed record AdminAiCountResponse(int Count);

public sealed record AdminAiRemoveResponse(int Removed, Guid JobId);

public sealed class AdminAiWorkflowService : IAdminAiWorkflowService
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly IBlogAiFixService _aiFixService;
    private readonly AiOptions _options;

    public AdminAiWorkflowService(
        WoongBlogDbContext dbContext,
        IBlogAiFixService aiFixService,
        IOptions<AiOptions> options)
    {
        _dbContext = dbContext;
        _aiFixService = aiFixService;
        _options = options.Value;
    }

    public AdminAiEndpoints.AiRuntimeConfigResponse RuntimeConfig()
    {
        return new AdminAiEndpoints.AiRuntimeConfigResponse(
            Provider: NormalizeProvider(_options.Provider),
            DefaultModel: NormalizeProvider(_options.Provider) switch
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
            BatchCompletedRetentionDays: _options.BatchCompletedRetentionDays);
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixResponse>> FixBlogAsync(AdminAiEndpoints.BlogFixRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixResponse>(null, new AdminAiFailure(AdminAiFailureKind.Validation, "HTML content is required."));
        }

        var result = await _aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.BlogFix,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort));
        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixResponse>(new AdminAiEndpoints.BlogFixResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchResponse>> FixBlogBatchAsync(AdminAiEndpoints.BlogFixBatchRequest request, CancellationToken cancellationToken)
    {
        var selection = AdminAiSelectionValidator.Validate(request.BlogIds, request.All);
        if (!selection.IsValid)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchResponse>(null, new AdminAiFailure(AdminAiFailureKind.Validation, selection.ErrorMessage!));
        }

        var query = _dbContext.Blogs.AsQueryable();
        if (!request.All)
        {
            query = query.Where(blog => selection.BlogIds.Contains(blog.Id));
        }

        var blogs = await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .ToListAsync(cancellationToken);

        var results = new List<AdminAiEndpoints.BlogFixBatchItemResponse>(blogs.Count);
        foreach (var blog in blogs)
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);

            try
            {
                var aiResult = await _aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                    Mode: AiFixMode.BlogFix,
                    CodexModel: request.CodexModel,
                    CodexReasoningEffort: request.CodexReasoningEffort));

                if (request.Apply)
                {
                    blog.ApplyFixedHtml(
                        $$"""{"html":{{JsonSerializer.Serialize(aiResult.FixedHtml)}}}""",
                        AdminContentText.GenerateExcerpt(aiResult.FixedHtml),
                        DateTimeOffset.UtcNow);
                }

                results.Add(new AdminAiEndpoints.BlogFixBatchItemResponse(
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
                results.Add(new AdminAiEndpoints.BlogFixBatchItemResponse(
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

        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchResponse>(new AdminAiEndpoints.BlogFixBatchResponse(results, request.Apply));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.WorkEnrichResponse>> EnrichWorkAsync(AdminAiEndpoints.WorkEnrichRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return new AdminAiOperationResult<AdminAiEndpoints.WorkEnrichResponse>(null, new AdminAiFailure(AdminAiFailureKind.Validation, "HTML content is required."));
        }

        var result = await _aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.WorkEnrich,
            Title: request.Title,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort));
        return new AdminAiOperationResult<AdminAiEndpoints.WorkEnrichResponse>(new AdminAiEndpoints.WorkEnrichResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>> CreateBlogFixBatchJobAsync(AdminAiEndpoints.BlogFixBatchJobCreateRequest request, CancellationToken cancellationToken)
    {
        var selection = AdminAiSelectionValidator.Validate(request.BlogIds, request.All);
        if (!selection.IsValid)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(null, new AdminAiFailure(AdminAiFailureKind.Validation, selection.ErrorMessage!));
        }

        var query = _dbContext.Blogs.AsQueryable();
        if (!request.All)
        {
            query = query.Where(blog => selection.BlogIds.Contains(blog.Id));
        }

        var blogs = await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .Select(blog => new { blog.Id, blog.Title })
            .ToListAsync(cancellationToken);

        if (blogs.Count == 0)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(null, new AdminAiFailure(AdminAiFailureKind.Validation, "No matching blogs were found."));
        }

        var runtimeProvider = NormalizeProvider(_options.Provider);
        var runtimeModel = runtimeProvider == "codex"
            ? ResolveCodexModel(_options, request.CodexModel)
            : runtimeProvider == "azure"
                ? _options.AzureOpenAiDeployment
                : _options.OpenAiModel;
        var runtimeReasoning = runtimeProvider == "codex"
            ? ResolveCodexReasoningEffort(_options, request.CodexReasoningEffort)
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

            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(ToJobDetailResponse(existingJob, existingItems, includeHtml: false));
        }

        var job = AiBatchJob.CreateBlogFixJob(
            request.SelectionMode ?? "selected",
            request.SelectionLabel ?? string.Empty,
            selectionKey,
            request.All,
            request.AutoApply,
            workerCount,
            blogs.Count,
            runtimeProvider,
            runtimeModel,
            runtimeReasoning,
            DateTimeOffset.UtcNow);

        var items = blogs.Select(blog => AiBatchJobItem.Create(job.Id, blog.Id, blog.Title, DateTimeOffset.UtcNow)).ToList();

        _dbContext.AiBatchJobs.Add(job);
        _dbContext.AiBatchJobItems.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(ToJobDetailResponse(job, items, includeHtml: false));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobListResponse>> ListBlogFixBatchJobsAsync(CancellationToken cancellationToken)
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

        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobListResponse>(new AdminAiEndpoints.BlogFixBatchJobListResponse(
            jobs,
            RunningCount: counts.Where(x => x.Status == AiBatchJobStates.Running).Select(x => x.Count).SingleOrDefault(),
            QueuedCount: counts.Where(x => x.Status == AiBatchJobStates.Queued).Select(x => x.Count).SingleOrDefault(),
            CompletedCount: counts.Where(x => x.Status == AiBatchJobStates.Completed).Select(x => x.Count).SingleOrDefault(),
            FailedCount: counts.Where(x => x.Status == AiBatchJobStates.Failed).Select(x => x.Count).SingleOrDefault(),
            CancelledCount: counts.Where(x => x.Status == AiBatchJobStates.Cancelled).Select(x => x.Count).SingleOrDefault()));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>> GetBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(null, new AdminAiFailure(AdminAiFailureKind.NotFound, "Job not found."));
        }

        var items = await _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(ToJobDetailResponse(job, items, includeHtml: true));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>> ApplyBlogFixBatchJobAsync(Guid jobId, AdminAiEndpoints.BlogFixBatchJobApplyRequest request, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(null, new AdminAiFailure(AdminAiFailureKind.NotFound, "Job not found."));
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

            blog.ApplyFixedHtml(
                $$"""{"html":{{JsonSerializer.Serialize(item.FixedHtml)}}}""",
                AdminContentText.GenerateExcerpt(item.FixedHtml),
                now);
            item.MarkApplied(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshJobAggregatesAsync(_dbContext, job, cancellationToken);

        var refreshedItems = await _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobDetailResponse>(ToJobDetailResponse(job, refreshedItems, includeHtml: true));
    }

    public async Task<AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobSummaryResponse>> CancelBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
        if (job is null)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobSummaryResponse>(null, new AdminAiFailure(AdminAiFailureKind.NotFound, "Job not found."));
        }

        if (job.Status is AiBatchJobStates.Completed or AiBatchJobStates.Failed or AiBatchJobStates.Cancelled)
        {
            return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobSummaryResponse>(ToJobSummaryResponse(job));
        }

        job.RequestCancel(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminAiOperationResult<AdminAiEndpoints.BlogFixBatchJobSummaryResponse>(ToJobSummaryResponse(job));
    }

    public async Task<AdminAiOperationResult<AdminAiCountResponse>> CancelQueuedBlogFixBatchJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Queued)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return new AdminAiOperationResult<AdminAiCountResponse>(new AdminAiCountResponse(0));
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await _dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId) && item.Status == AiBatchJobItemStates.Pending)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            job.Cancel(now);
        }

        foreach (var item in items)
        {
            item.Cancel(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminAiOperationResult<AdminAiCountResponse>(new AdminAiCountResponse(jobs.Count));
    }

    public async Task<AdminAiOperationResult<AdminAiCountResponse>> ClearCompletedBlogFixBatchJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Completed)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return new AdminAiOperationResult<AdminAiCountResponse>(new AdminAiCountResponse(0));
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await _dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId))
            .ToListAsync(cancellationToken);

        _dbContext.AiBatchJobItems.RemoveRange(items);
        _dbContext.AiBatchJobs.RemoveRange(jobs);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminAiOperationResult<AdminAiCountResponse>(new AdminAiCountResponse(jobs.Count));
    }

    public async Task<AdminAiOperationResult<AdminAiRemoveResponse>> RemoveBlogFixBatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.AiBatchJobs
            .SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);

        if (job is null)
        {
            return new AdminAiOperationResult<AdminAiRemoveResponse>(null, new AdminAiFailure(AdminAiFailureKind.NotFound, "Job not found."));
        }

        if (job.Status is AiBatchJobStates.Queued or AiBatchJobStates.Running)
        {
            return new AdminAiOperationResult<AdminAiRemoveResponse>(null, new AdminAiFailure(AdminAiFailureKind.Conflict, "Only completed, failed, or cancelled jobs can be removed."));
        }

        var items = await _dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .ToListAsync(cancellationToken);

        _dbContext.AiBatchJobItems.RemoveRange(items);
        _dbContext.AiBatchJobs.Remove(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminAiOperationResult<AdminAiRemoveResponse>(new AdminAiRemoveResponse(1, jobId));
    }

    private static string NormalizeProvider(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "azureopenai" => "azure",
            "azure-openai" => "azure",
            "azure" => "azure",
            "codex" => "codex",
            _ => "openai"
        };
    }

    private static string ResolveCodexModel(AiOptions options, string? overrideValue)
    {
        string candidate;
        if (string.IsNullOrWhiteSpace(overrideValue))
        {
            candidate = options.CodexModel;
        }
        else
        {
            candidate = overrideValue.Trim();
        }

        if (options.CodexAllowedModels.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            return candidate;
        }

        return options.CodexModel;
    }

    private static string ResolveCodexReasoningEffort(AiOptions options, string? overrideValue)
    {
        string candidate;
        if (string.IsNullOrWhiteSpace(overrideValue))
        {
            candidate = options.CodexReasoningEffort;
        }
        else
        {
            candidate = overrideValue.Trim().ToLowerInvariant();
        }

        if (options.CodexAllowedReasoningEfforts.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            return candidate;
        }

        return options.CodexReasoningEffort;
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
        var normalizedWorkerCount = NormalizeWorkerCount(workerCount);
        var orderedIds = blogIds.OrderBy(id => id).ToArray();
        var parts = new List<string>
        {
            selectionMode ?? "selected",
            all ? "all" : "subset",
            runtimeModel,
            runtimeReasoning ?? string.Empty,
            autoApply ? "auto-apply" : "manual-apply",
            normalizedWorkerCount?.ToString() ?? "default-workers",
            string.Join(",", orderedIds)
        };
        var canonical = string.Join('|', parts);
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

    private static AdminAiEndpoints.BlogFixBatchJobSummaryResponse ToJobSummaryResponse(AiBatchJob job) => new(
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

    private static AdminAiEndpoints.BlogFixBatchJobDetailResponse ToJobDetailResponse(AiBatchJob job, IReadOnlyList<AiBatchJobItem> items, bool includeHtml) => new(
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
        items.Select(item => new AdminAiEndpoints.BlogFixBatchJobItemResponse(
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

    private static async Task RefreshJobAggregatesAsync(WoongBlogDbContext dbContext, AiBatchJob job, CancellationToken cancellationToken)
    {
        var items = await dbContext.AiBatchJobItems.Where(item => item.JobId == job.Id).ToListAsync(cancellationToken);
        var totalCount = items.Count;
        var processedCount = 0;
        var succeededCount = 0;
        var failedCount = 0;

        foreach (var item in items)
        {
            if (item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Failed or AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled)
            {
                processedCount += 1;
            }

            if (item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Applied)
            {
                succeededCount += 1;
            }

            if (item.Status == AiBatchJobItemStates.Failed)
            {
                failedCount += 1;
            }
        }

        job.RefreshCounts(totalCount, processedCount, succeededCount, failedCount, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
