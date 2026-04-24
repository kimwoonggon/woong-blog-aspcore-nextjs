using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.AI.Abstractions;
using WoongBlog.Application.Modules.AI.BatchJobs;

namespace WoongBlog.Infrastructure.Ai;

public sealed class AiBatchJobItemDispatcher(IServiceScopeFactory scopeFactory) : IAiBatchJobItemDispatcher
{
    public async Task ProcessQueuedItemAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var jobQueryStore = scope.ServiceProvider.GetRequiredService<IAiBatchJobQueryStore>();
        var commandStore = scope.ServiceProvider.GetRequiredService<IAiBatchJobCommandStore>();

        if (await IsCancellationRequestedAsync(jobQueryStore, jobId, cancellationToken))
        {
            await MarkPendingItemCancelledAsync(jobQueryStore, commandStore, itemId, cancellationToken);
            return;
        }

        var itemProcessor = scope.ServiceProvider.GetRequiredService<IAiBatchJobItemProcessor>();
        await itemProcessor.ProcessAsync(jobId, itemId, cancellationToken);
        await RefreshJobCountsAsync(jobQueryStore, commandStore, jobId, cancellationToken);
    }

    public async Task FinalizeJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var jobQueryStore = scope.ServiceProvider.GetRequiredService<IAiBatchJobQueryStore>();
        var commandStore = scope.ServiceProvider.GetRequiredService<IAiBatchJobCommandStore>();
        var job = await jobQueryStore.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var items = await jobQueryStore.GetJobItemsAsync(jobId, cancellationToken);
        AiBatchJobProgressPolicy.Finalize(job, items, DateTimeOffset.UtcNow);
        await commandStore.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> IsCancellationRequestedAsync(
        IAiBatchJobQueryStore jobQueryStore,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return await jobQueryStore.GetBlogJobAsync(jobId, cancellationToken) is { CancelRequested: true };
    }

    private static async Task MarkPendingItemCancelledAsync(
        IAiBatchJobQueryStore jobQueryStore,
        IAiBatchJobCommandStore commandStore,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await jobQueryStore.GetJobItemAsync(itemId, cancellationToken);
        if (item is null || item.Status != AiBatchJobItemStates.Pending)
        {
            return;
        }

        AiBatchJobProgressPolicy.MarkCancelled(item, DateTimeOffset.UtcNow);
        await commandStore.SaveChangesAsync(cancellationToken);
    }

    private static async Task RefreshJobCountsAsync(
        IAiBatchJobQueryStore jobQueryStore,
        IAiBatchJobCommandStore commandStore,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await jobQueryStore.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var items = await jobQueryStore.GetJobItemsAsync(jobId, cancellationToken);
        AiBatchJobProgressPolicy.RefreshCounts(job, items, DateTimeOffset.UtcNow);
        await commandStore.SaveChangesAsync(cancellationToken);
    }
}
