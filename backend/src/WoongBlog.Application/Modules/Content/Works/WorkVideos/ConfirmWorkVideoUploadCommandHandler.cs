using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Works.Support;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed class ConfirmWorkVideoUploadCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore,
    IWorkVideoStorageSelector storageSelector,
    IWorkVideoFileInspector fileInspector)
    : IRequestHandler<ConfirmWorkVideoUploadCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        ConfirmWorkVideoUploadCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var session = await commandStore.GetUploadSessionForUpdateAsync(request.WorkId, request.UploadSessionId, cancellationToken);
        if (session is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Upload session not found.");
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            session.Status = WorkVideoUploadSessionStatuses.Expired;
            await commandStore.SaveChangesAsync(cancellationToken);
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Upload session expired.");
        }

        if (!storageSelector.TryGetStorage(session.StorageType, out var storage))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Video storage backend is not available.");
        }

        var storedObject = await storage.GetObjectAsync(session.StorageKey, cancellationToken);
        if (storedObject is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Uploaded object was not found.");
        }

        if (!string.Equals(session.ExpectedMimeType, storedObject.ContentType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(session.StorageType, WorkVideoSourceTypes.Local, StringComparison.OrdinalIgnoreCase))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Uploaded object content type did not match.");
        }

        if (storedObject.Size != session.ExpectedSize)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Uploaded object size did not match.");
        }

        if (!await fileInspector.LooksLikeMp4Async(session.StorageKey, storage, cancellationToken))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Only valid MP4 files are supported.");
        }

        var videos = (await commandStore.GetVideosForWorkAsync(request.WorkId, cancellationToken)).ToList();
        if (videos.Count >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var assetPublicUrls = await commandStore.GetAssetPublicUrlsAsync(
            WorkPublicThumbnailReadModel.GetThumbnailAssetIds(work.ThumbnailAssetId),
            cancellationToken);

        var video = new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = request.WorkId,
            SourceType = session.StorageType,
            SourceKey = session.StorageKey,
            OriginalFileName = session.OriginalFileName,
            MimeType = storedObject.ContentType ?? session.ExpectedMimeType,
            FileSize = storedObject.Size,
            SortOrder = GetNextSortOrder(videos),
            CreatedAt = DateTimeOffset.UtcNow
        };
        commandStore.AddWorkVideo(video);
        videos.Add(video);
        WorkPublicThumbnailReadModel.Refresh(
            work,
            WorkPublicThumbnailReadModel.ShouldLoadFallbackVideos(work.ThumbnailAssetId, assetPublicUrls)
                ? videos
                : Array.Empty<WorkVideo>(),
            assetPublicUrls);
        WorkPublicVideosReadModel.Refresh(work, videos);

        session.Status = WorkVideoUploadSessionStatuses.Confirmed;
        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }

    private static int GetNextSortOrder(IReadOnlyCollection<WorkVideo> videos)
    {
        return videos.Count == 0 ? 0 : videos.Max(video => video.SortOrder) + 1;
    }
}
