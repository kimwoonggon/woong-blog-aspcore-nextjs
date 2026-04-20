using MediatR;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Api;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

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
