using Microsoft.Extensions.Options;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

public sealed class AiBatchJobRunner(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchJobItemDispatcher itemDispatcher,
    IOptions<AiOptions> options) : IAiBatchJobRunner
{
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
                while (TryDequeue(queue, gate, out var itemId))
                {
                    await itemDispatcher.ProcessQueuedItemAsync(jobId, itemId, cancellationToken);
                }
            }, cancellationToken));

        await Task.WhenAll(workers);
        await itemDispatcher.FinalizeJobAsync(jobId, cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> GetPendingItemIdsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var pendingItems = await jobQueryStore.GetPendingItemsForJobsAsync([jobId], cancellationToken);
        return pendingItems.Select(item => item.Id).ToArray();
    }

    private async Task<int> ResolveWorkerCountAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobQueryStore.GetBlogJobAsync(jobId, cancellationToken);
        return AiBatchWorkerPolicy.ResolveWorkerCount(job?.WorkerCount, _options.BatchConcurrency);
    }

    private static bool TryDequeue(Queue<Guid> queue, object gate, out Guid itemId)
    {
        lock (gate)
        {
            if (queue.Count == 0)
            {
                itemId = Guid.Empty;
                return false;
            }

            itemId = queue.Dequeue();
            return true;
        }
    }
}
