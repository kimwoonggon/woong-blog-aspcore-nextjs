using System.Text.Json;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public interface IAiBatchJobRunner
{
    Task RunAsync(Guid jobId, CancellationToken cancellationToken);
}

public interface IAiBatchJobItemProcessor
{
    Task ProcessAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken);
}

public interface IBlogFixApplyPolicy
{
    void Apply(Blog blog, AiBatchJobItem item, string fixedHtml, DateTimeOffset timestamp);
}

public sealed class BlogFixApplyPolicy : IBlogFixApplyPolicy
{
    public void Apply(Blog blog, AiBatchJobItem item, string fixedHtml, DateTimeOffset timestamp)
    {
        blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(fixedHtml)}}}""";
        blog.Excerpt = AdminContentText.GenerateExcerpt(fixedHtml);
        blog.UpdatedAt = timestamp;
        item.AppliedAt = timestamp;
        item.Status = AiBatchJobItemStates.Applied;
    }
}

public static class AiBatchWorkerPolicy
{
    public static int ResolveWorkerCount(int? jobWorkerCount, int configuredDefault)
    {
        return Math.Max(1, jobWorkerCount ?? configuredDefault);
    }
}

public sealed class AiBatchJobRunner(
    IServiceScopeFactory scopeFactory,
    IOptions<AiOptions> options) : IAiBatchJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AiOptions _options = options.Value;

    public async Task RunAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var itemIds = await GetPendingItemIdsAsync(jobId, cancellationToken);
        var workerCount = await ResolveWorkerCountAsync(jobId, cancellationToken);
        var queue = new Queue<Guid>(itemIds);
        var gate = new object();
        var workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                while (true)
                {
                    Guid itemId;
                    lock (gate)
                    {
                        if (queue.Count == 0)
                        {
                            return;
                        }

                        itemId = queue.Dequeue();
                    }

                    if (await IsCancellationRequestedAsync(jobId, cancellationToken))
                    {
                        await MarkPendingItemCancelledAsync(itemId, cancellationToken);
                        continue;
                    }

                    await ProcessItemAsync(jobId, itemId, cancellationToken);
                    await RefreshJobCountsAsync(jobId, cancellationToken);
                }
            }, cancellationToken));

        await Task.WhenAll(workers);
        await FinalizeJobAsync(jobId, cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> GetPendingItemIdsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        var pendingItems = await store.GetPendingItemsForJobsAsync([jobId], cancellationToken);
        return pendingItems.Select(item => item.Id).ToArray();
    }

    private async Task<int> ResolveWorkerCountAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        var job = await store.GetBlogJobAsync(jobId, cancellationToken);
        return AiBatchWorkerPolicy.ResolveWorkerCount(job?.WorkerCount, _options.BatchConcurrency);
    }

    private async Task<bool> IsCancellationRequestedAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        return await store.GetBlogJobAsync(jobId, cancellationToken) is { CancelRequested: true };
    }

    private async Task MarkPendingItemCancelledAsync(Guid itemId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        var item = await store.GetJobItemAsync(itemId, cancellationToken);
        if (item is null || item.Status != AiBatchJobItemStates.Pending)
        {
            return;
        }

        AiBatchJobProgressPolicy.MarkCancelled(item, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessItemAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var itemProcessor = scope.ServiceProvider.GetRequiredService<IAiBatchJobItemProcessor>();
        await itemProcessor.ProcessAsync(jobId, itemId, cancellationToken);
    }

    private async Task RefreshJobCountsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        var job = await store.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var items = await store.GetJobItemsAsync(jobId, cancellationToken);
        AiBatchJobProgressPolicy.RefreshCounts(job, items, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task FinalizeJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        var job = await store.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var items = await store.GetJobItemsAsync(jobId, cancellationToken);
        AiBatchJobProgressPolicy.Finalize(job, items, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AiBatchJobItemProcessor(
    IAiBlogFixBatchStore store,
    IBlogAiFixService aiFixService,
    IBlogFixApplyPolicy applyPolicy) : IAiBatchJobItemProcessor
{
    public async Task ProcessAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await store.GetJobItemAsync(itemId, cancellationToken);
        if (item is null)
        {
            return;
        }

        var job = await store.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            AiBatchJobProgressPolicy.MarkFailed(item, "Batch job no longer exists.", DateTimeOffset.UtcNow);
            await store.SaveChangesAsync(cancellationToken);
            return;
        }

        var blogLookup = await store.GetBlogsForUpdateAsync([item.EntityId], cancellationToken);
        if (!blogLookup.TryGetValue(item.EntityId, out var blog))
        {
            AiBatchJobProgressPolicy.MarkFailed(item, "Target blog no longer exists.", DateTimeOffset.UtcNow);
            await store.SaveChangesAsync(cancellationToken);
            return;
        }

        AiBatchJobProgressPolicy.MarkRunning(item, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        try
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);
            var result = await aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                Mode: AiFixMode.BlogFix,
                Provider: job.Provider,
                CodexModel: job.Model,
                CodexReasoningEffort: job.ReasoningEffort,
                CustomPrompt: job.CustomPrompt));

            item.FixedHtml = result.FixedHtml;
            item.Provider = result.Provider;
            item.Model = result.Model;
            item.ReasoningEffort = result.ReasoningEffort;
            item.Error = null;

            if (job.AutoApply)
            {
                applyPolicy.Apply(blog, item, result.FixedHtml, DateTimeOffset.UtcNow);
            }
            else
            {
                item.Status = AiBatchJobItemStates.Succeeded;
            }
        }
        catch (Exception exception)
        {
            AiBatchJobProgressPolicy.MarkFailed(item, exception.Message, DateTimeOffset.UtcNow);
        }

        item.FinishedAt = DateTimeOffset.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
    }
}
