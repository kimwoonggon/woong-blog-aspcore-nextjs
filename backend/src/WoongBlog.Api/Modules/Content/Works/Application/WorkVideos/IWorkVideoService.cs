using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoService
{
    Task<WorkVideoServiceResult<VideoUploadTargetResult>> IssueUploadAsync(
        Guid workId,
        string fileName,
        string contentType,
        long size,
        int expectedVideosVersion,
        CancellationToken cancellationToken);
    Task<WorkVideoServiceResult<object>> UploadLocalAsync(
        Guid workId,
        Guid uploadSessionId,
        IFormFile? file,
        CancellationToken cancellationToken);
    Task<WorkVideoServiceResult<WorkVideosMutationResult>> UploadHlsAsync(
        Guid workId,
        IFormFile? file,
        int expectedVideosVersion,
        CancellationToken cancellationToken);
    Task<WorkVideoServiceResult<WorkVideosMutationResult>> ConfirmUploadAsync(
        Guid workId,
        Guid uploadSessionId,
        int expectedVideosVersion,
        CancellationToken cancellationToken);
    Task<WorkVideoServiceResult<WorkVideosMutationResult>> AddYouTubeAsync(
        Guid workId,
        string youtubeUrlOrId,
        int expectedVideosVersion,
        CancellationToken cancellationToken);
    Task<WorkVideoServiceResult<WorkVideosMutationResult>> ReorderAsync(
        Guid workId,
        IReadOnlyList<Guid> orderedVideoIds,
        int expectedVideosVersion,
        CancellationToken cancellationToken);
    Task<WorkVideoServiceResult<WorkVideosMutationResult>> DeleteAsync(
        Guid workId,
        Guid videoId,
        int expectedVideosVersion,
        CancellationToken cancellationToken);
    Task EnqueueCleanupForWorkAsync(Guid workId, CancellationToken cancellationToken);
    Task<int> ProcessCleanupJobsAsync(CancellationToken cancellationToken);
    Task<int> ExpireUploadSessionsAsync(CancellationToken cancellationToken);
}
