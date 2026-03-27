using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Domain.Entities;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Infrastructure.Ai;

public sealed class AiBatchJobProcessor(IServiceScopeFactory scopeFactory, IOptions<AiOptions> options) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AiOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            var runningJobs = await dbContext.AiBatchJobs
                .Where(job => job.Status == AiBatchJobStates.Running)
                .ToListAsync(stoppingToken);

            foreach (var job in runningJobs)
            {
                job.Status = AiBatchJobStates.Queued;
                job.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (runningJobs.Count > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            await PruneCompletedJobsAsync(dbContext, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid? nextJobId = null;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
                var nextJob = await dbContext.AiBatchJobs
                    .Where(job => job.Status == AiBatchJobStates.Queued)
                    .OrderBy(job => job.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (nextJob is not null)
                {
                    nextJob.Status = AiBatchJobStates.Running;
                    nextJob.StartedAt = DateTimeOffset.UtcNow;
                    nextJob.UpdatedAt = DateTimeOffset.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    nextJobId = nextJob.Id;
                }
            }

            if (nextJobId is null)
            {
                using var pruneScope = _scopeFactory.CreateScope();
                var pruneDbContext = pruneScope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
                await PruneCompletedJobsAsync(pruneDbContext, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            await ProcessJobAsync(nextJobId.Value, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> itemIds;

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            itemIds = await dbContext.AiBatchJobItems
                .Where(item => item.JobId == jobId && item.Status == AiBatchJobItemStates.Pending)
                .OrderBy(item => item.CreatedAt)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);
        }

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

    private async Task<int> ResolveWorkerCountAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var configured = await dbContext.AiBatchJobs
            .Where(job => job.Id == jobId)
            .Select(job => job.WorkerCount)
            .SingleAsync(cancellationToken);

        return Math.Max(1, configured ?? _options.BatchConcurrency);
    }

    private async Task ProcessItemAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var aiFixService = scope.ServiceProvider.GetRequiredService<IBlogAiFixService>();

        var item = await dbContext.AiBatchJobItems.SingleAsync(x => x.Id == itemId, cancellationToken);
        var job = await dbContext.AiBatchJobs.SingleAsync(x => x.Id == jobId, cancellationToken);
        var blog = await dbContext.Blogs.SingleAsync(x => x.Id == item.EntityId, cancellationToken);

        item.Status = AiBatchJobItemStates.Running;
        item.StartedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);
            var result = await aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                Mode: AiFixMode.BlogFix,
                CodexModel: job.Model,
                CodexReasoningEffort: job.ReasoningEffort));

            item.FixedHtml = result.FixedHtml;
            item.Provider = result.Provider;
            item.Model = result.Model;
            item.ReasoningEffort = result.ReasoningEffort;
            item.Error = null;

            if (job.AutoApply)
            {
                var now = DateTimeOffset.UtcNow;
                blog.ContentJson = $$"""{"html":{{System.Text.Json.JsonSerializer.Serialize(result.FixedHtml)}}}""";
                blog.Excerpt = AdminContentText.GenerateExcerpt(result.FixedHtml);
                blog.UpdatedAt = now;
                item.AppliedAt = now;
                item.Status = AiBatchJobItemStates.Applied;
            }
            else
            {
                item.Status = AiBatchJobItemStates.Succeeded;
            }
        }
        catch (Exception exception)
        {
            item.Status = AiBatchJobItemStates.Failed;
            item.Error = exception.Message;
        }

        item.FinishedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsCancellationRequestedAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        return await dbContext.AiBatchJobs
            .Where(job => job.Id == jobId)
            .Select(job => job.CancelRequested)
            .SingleAsync(cancellationToken);
    }

    private async Task MarkPendingItemCancelledAsync(Guid itemId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var item = await dbContext.AiBatchJobItems.SingleAsync(x => x.Id == itemId, cancellationToken);
        if (item.Status == AiBatchJobItemStates.Pending)
        {
            item.Status = AiBatchJobItemStates.Cancelled;
            item.FinishedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RefreshJobCountsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var job = await dbContext.AiBatchJobs.SingleAsync(x => x.Id == jobId, cancellationToken);
        var items = await dbContext.AiBatchJobItems.Where(x => x.JobId == jobId).ToListAsync(cancellationToken);

        job.TotalCount = items.Count;
        job.ProcessedCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Failed or AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled);
        job.SucceededCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Applied);
        job.FailedCount = items.Count(item => item.Status == AiBatchJobItemStates.Failed);
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task FinalizeJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var job = await dbContext.AiBatchJobs.SingleAsync(x => x.Id == jobId, cancellationToken);
        var items = await dbContext.AiBatchJobItems.Where(x => x.JobId == jobId).ToListAsync(cancellationToken);

        job.TotalCount = items.Count;
        job.ProcessedCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Failed or AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled);
        job.SucceededCount = items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Applied);
        job.FailedCount = items.Count(item => item.Status == AiBatchJobItemStates.Failed);
        job.Status = job.CancelRequested ? AiBatchJobStates.Cancelled :
            job.FailedCount == job.TotalCount && job.TotalCount > 0 ? AiBatchJobStates.Failed :
            AiBatchJobStates.Completed;
        job.FinishedAt = DateTimeOffset.UtcNow;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PruneCompletedJobsAsync(PortfolioDbContext dbContext, CancellationToken cancellationToken)
    {
        if (_options.BatchCompletedRetentionDays < 0)
        {
            return;
        }

        var threshold = DateTimeOffset.UtcNow.AddDays(-_options.BatchCompletedRetentionDays);
        var oldCompletedJobs = await dbContext.AiBatchJobs
            .Where(job =>
                job.TargetType == "blog"
                && job.Status == AiBatchJobStates.Completed
                && job.FinishedAt != null
                && job.FinishedAt < threshold)
            .Select(job => job.Id)
            .ToListAsync(cancellationToken);

        if (oldCompletedJobs.Count == 0)
        {
            return;
        }

        var items = await dbContext.AiBatchJobItems
            .Where(item => oldCompletedJobs.Contains(item.JobId))
            .ToListAsync(cancellationToken);
        var jobs = await dbContext.AiBatchJobs
            .Where(job => oldCompletedJobs.Contains(job.Id))
            .ToListAsync(cancellationToken);

        dbContext.AiBatchJobItems.RemoveRange(items);
        dbContext.AiBatchJobs.RemoveRange(jobs);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
