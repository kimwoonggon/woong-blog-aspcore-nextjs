using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Api;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class CreateBlogFixBatchJobCommandHandler(
    IAiBlogFixBatchStore store,
    AiBatchJobSignal batchJobSignal,
    IOptions<AiOptions> options) : IRequestHandler<CreateBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobDetailResponse>>
{
    private readonly AiOptions _options = options.Value;

    public async Task<AiActionResult<BlogFixBatchJobDetailResponse>> Handle(
        CreateBlogFixBatchJobCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.All && (request.BlogIds is null || request.BlogIds.Count == 0))
        {
            return AiActionResult<BlogFixBatchJobDetailResponse>.BadRequest("Either blogIds or all=true is required.");
        }

        var blogs = await store.GetBlogTargetsAsync(request.All, request.BlogIds, cancellationToken);
        if (blogs.Count == 0)
        {
            return AiActionResult<BlogFixBatchJobDetailResponse>.BadRequest("No matching blogs were found.");
        }

        var runtimeProvider = AiRuntimePolicy.ResolveRequestedProvider(_options, request.Provider);
        var runtimeModel = runtimeProvider == "codex"
            ? AiRuntimePolicy.ResolveCodexModel(_options, request.CodexModel)
            : runtimeProvider == "azure"
                ? _options.AzureOpenAiDeployment
                : _options.OpenAiModel;
        var runtimeReasoning = runtimeProvider == "codex"
            ? AiRuntimePolicy.ResolveCodexReasoningEffort(_options, request.CodexReasoningEffort)
            : null;
        var customPrompt = AiRuntimePolicy.NormalizeCustomPrompt(request.CustomPrompt);
        var selectionKey = AiRuntimePolicy.BuildSelectionKey(
            request.SelectionMode,
            blogs.Select(blog => blog.Id).ToArray(),
            runtimeModel,
            runtimeReasoning,
            customPrompt,
            request.All,
            request.AutoApply,
            request.WorkerCount);
        var workerCount = AiRuntimePolicy.NormalizeWorkerCount(request.WorkerCount);

        var existingJob = await store.GetActiveBlogJobBySelectionKeyAsync(selectionKey, cancellationToken);
        if (existingJob is not null)
        {
            var existingItems = await store.GetJobItemsAsync(existingJob.Id, cancellationToken);
            if (existingJob.Status == AiBatchJobStates.Queued)
            {
                batchJobSignal.Notify();
            }

            return AiActionResult<BlogFixBatchJobDetailResponse>.Ok(
                AiBatchJobResponseMapper.ToJobDetailResponse(existingJob, existingItems, includeHtml: false));
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

        store.AddJob(job, items);
        await store.SaveChangesAsync(cancellationToken);
        batchJobSignal.Notify();

        return AiActionResult<BlogFixBatchJobDetailResponse>.Ok(
            AiBatchJobResponseMapper.ToJobDetailResponse(job, items, includeHtml: false));
    }
}

public sealed class ListBlogFixBatchJobsQueryHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<ListBlogFixBatchJobsQuery, BlogFixBatchJobListResponse>
{
    public async Task<BlogFixBatchJobListResponse> Handle(ListBlogFixBatchJobsQuery request, CancellationToken cancellationToken)
    {
        var counts = await store.GetBlogJobCountsAsync(cancellationToken);
        var jobs = await store.GetRecentBlogJobsAsync(take: 20, cancellationToken);

        return new BlogFixBatchJobListResponse(
            jobs.Select(AiBatchJobResponseMapper.ToJobSummaryResponse).ToList(),
            counts.RunningCount,
            counts.QueuedCount,
            counts.CompletedCount,
            counts.FailedCount,
            counts.CancelledCount);
    }
}

public sealed class GetBlogFixBatchJobQueryHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<GetBlogFixBatchJobQuery, BlogFixBatchJobDetailResponse?>
{
    public async Task<BlogFixBatchJobDetailResponse?> Handle(GetBlogFixBatchJobQuery request, CancellationToken cancellationToken)
    {
        var job = await store.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var items = await store.GetJobItemsAsync(request.JobId, cancellationToken);
        return AiBatchJobResponseMapper.ToJobDetailResponse(job, items, includeHtml: true);
    }
}

public sealed class ApplyBlogFixBatchJobCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<ApplyBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobDetailResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobDetailResponse>> Handle(
        ApplyBlogFixBatchJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await store.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobDetailResponse>.NotFound();
        }

        var items = await store.GetSucceededUnappliedItemsAsync(request.JobId, request.JobItemIds, cancellationToken);
        var blogIds = items.Select(item => item.EntityId).ToArray();
        var blogs = await store.GetBlogsForUpdateAsync(blogIds, cancellationToken);
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

        await store.SaveChangesAsync(cancellationToken);
        await RefreshJobAggregatesAsync(store, job, cancellationToken);

        var refreshedItems = await store.GetJobItemsAsync(request.JobId, cancellationToken);
        return AiActionResult<BlogFixBatchJobDetailResponse>.Ok(
            AiBatchJobResponseMapper.ToJobDetailResponse(job, refreshedItems, includeHtml: true));
    }

    private static async Task RefreshJobAggregatesAsync(
        IAiBlogFixBatchStore store,
        AiBatchJob job,
        CancellationToken cancellationToken)
    {
        var items = await store.GetJobItemsAsync(job.Id, cancellationToken);
        AiBatchJobProgressPolicy.RefreshCounts(job, items, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }
}

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

public sealed class CancelQueuedBlogFixBatchJobsCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<CancelQueuedBlogFixBatchJobsCommand, BlogFixBatchJobCancelQueuedResponse>
{
    public async Task<BlogFixBatchJobCancelQueuedResponse> Handle(CancelQueuedBlogFixBatchJobsCommand request, CancellationToken cancellationToken)
    {
        var jobs = await store.GetQueuedBlogJobsAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            return new BlogFixBatchJobCancelQueuedResponse(0);
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await store.GetPendingItemsForJobsAsync(jobIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            job.CancelRequested = true;
            AiBatchJobProgressPolicy.MarkCancelled(job, now);
        }

        foreach (var item in items)
        {
            AiBatchJobProgressPolicy.MarkCancelled(item, now);
        }

        await store.SaveChangesAsync(cancellationToken);
        return new BlogFixBatchJobCancelQueuedResponse(jobs.Count);
    }
}

public sealed class ClearCompletedBlogFixBatchJobsCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<ClearCompletedBlogFixBatchJobsCommand, BlogFixBatchJobClearCompletedResponse>
{
    public async Task<BlogFixBatchJobClearCompletedResponse> Handle(ClearCompletedBlogFixBatchJobsCommand request, CancellationToken cancellationToken)
    {
        var jobs = await store.GetCompletedBlogJobsAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            return new BlogFixBatchJobClearCompletedResponse(0);
        }

        var jobIds = jobs.Select(job => job.Id).ToArray();
        var items = await store.GetItemsForJobsAsync(jobIds, cancellationToken);

        store.RemoveItems(items);
        store.RemoveJobs(jobs);
        await store.SaveChangesAsync(cancellationToken);

        return new BlogFixBatchJobClearCompletedResponse(jobs.Count);
    }
}

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
