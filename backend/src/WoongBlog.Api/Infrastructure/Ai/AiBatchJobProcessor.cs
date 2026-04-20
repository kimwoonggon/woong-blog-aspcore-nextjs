using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;

namespace WoongBlog.Api.Infrastructure.Ai;

public sealed class AiBatchJobProcessor(
    IServiceScopeFactory scopeFactory,
    AiBatchJobSignal signal) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AiBatchJobSignal _signal = signal;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResetRunningJobsAsync(stoppingToken);
        await ProcessQueuedJobsUntilEmptyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _signal.WaitAsync(stoppingToken);
            await ProcessQueuedJobsUntilEmptyAsync(stoppingToken);
        }
    }

    private async Task ResetRunningJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
        var runningJobs = await store.GetRunningBlogJobsAsync(cancellationToken);
        if (runningJobs.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var job in runningJobs)
        {
            AiBatchJobProgressPolicy.MarkQueued(job, now);
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessQueuedJobsUntilEmptyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AiBatchJob? nextJob = null;

            using (var scope = _scopeFactory.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<IAiBlogFixBatchStore>();
                nextJob = (await store.GetQueuedBlogJobsAsync(cancellationToken)).FirstOrDefault();
                if (nextJob is not null)
                {
                    AiBatchJobProgressPolicy.MarkRunning(nextJob, DateTimeOffset.UtcNow);
                    await store.SaveChangesAsync(cancellationToken);
                }
            }

            if (nextJob is null)
            {
                return;
            }

            using var jobScope = _scopeFactory.CreateScope();
            var runner = jobScope.ServiceProvider.GetRequiredService<IAiBatchJobRunner>();
            await runner.RunAsync(nextJob.Id, cancellationToken);
        }
    }
}
