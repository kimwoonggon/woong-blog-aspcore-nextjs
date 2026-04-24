using Microsoft.Extensions.Hosting;

namespace WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

public sealed class VideoStorageCleanupWorker(
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IWorkVideoCleanupService>();
            await service.ExpireUploadSessionsAsync(stoppingToken);
            await service.ProcessCleanupJobsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
