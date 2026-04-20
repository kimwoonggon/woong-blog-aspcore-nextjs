namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoCleanupService
{
    Task EnqueueCleanupForWorkAsync(Guid workId, CancellationToken cancellationToken);
    Task<int> ProcessCleanupJobsAsync(CancellationToken cancellationToken);
    Task<int> ExpireUploadSessionsAsync(CancellationToken cancellationToken);
}
