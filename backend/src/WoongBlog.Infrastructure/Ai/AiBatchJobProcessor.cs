using Microsoft.Extensions.Hosting;
using WoongBlog.Application.Modules.AI.BatchJobs;

namespace WoongBlog.Infrastructure.Ai;

public sealed class AiBatchJobProcessor(
    IServiceScopeFactory scopeFactory,
    AiBatchJobSignal signal) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AiBatchJobSignal _signal = signal;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunWithSchedulerAsync(
            scheduler => scheduler.ResetRunningJobsAsync(stoppingToken),
            stoppingToken);
        await RunWithSchedulerAsync(
            scheduler => scheduler.ProcessQueuedJobsUntilEmptyAsync(stoppingToken),
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _signal.WaitAsync(stoppingToken);
            await RunWithSchedulerAsync(
                scheduler => scheduler.ProcessQueuedJobsUntilEmptyAsync(stoppingToken),
                stoppingToken);
        }
    }

    private async Task RunWithSchedulerAsync(
        Func<IAiBatchJobScheduler, Task> action,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IAiBatchJobScheduler>();
        await action(scheduler);
    }
}
