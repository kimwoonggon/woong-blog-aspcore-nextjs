using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;

namespace WoongBlog.Api.Infrastructure.Ai;

public sealed class AiBatchJobItemDispatcher(IServiceScopeFactory scopeFactory) : IAiBatchJobItemDispatcher
{
    public async Task ProcessQueuedItemAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();

        if (await IsCancellationRequestedAsync(store, jobId, cancellationToken))
        {
            await MarkPendingItemCancelledAsync(store, itemId, cancellationToken);
            return;
        }

        var itemProcessor = scope.ServiceProvider.GetRequiredService<IAiBatchJobItemProcessor>();
        await itemProcessor.ProcessAsync(jobId, itemId, cancellationToken);
        await RefreshJobCountsAsync(store, jobId, cancellationToken);
    }

    public async Task FinalizeJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
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

    private static async Task<bool> IsCancellationRequestedAsync(
        IAiBlogFixBatchStore store,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return await store.GetBlogJobAsync(jobId, cancellationToken) is { CancelRequested: true };
    }

    private static async Task MarkPendingItemCancelledAsync(
        IAiBlogFixBatchStore store,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await store.GetJobItemAsync(itemId, cancellationToken);
        if (item is null || item.Status != AiBatchJobItemStates.Pending)
        {
            return;
        }

        AiBatchJobProgressPolicy.MarkCancelled(item, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }

    private static async Task RefreshJobCountsAsync(
        IAiBlogFixBatchStore store,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await store.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var items = await store.GetJobItemsAsync(jobId, cancellationToken);
        AiBatchJobProgressPolicy.RefreshCounts(job, items, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }
}
