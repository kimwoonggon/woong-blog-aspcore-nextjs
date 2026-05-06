using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Works.Support;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed class StartWorkVideoHlsJobCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore,
    IWorkVideoStorageSelector storageSelector,
    IVideoTranscoder videoTranscoder,
    IWorkVideoFileInspector fileInspector,
    IWorkVideoHlsWorkspace hlsWorkspace,
    IWorkVideoHlsOutputPublisher hlsOutputPublisher)
    : IRequestHandler<StartWorkVideoHlsJobCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        StartWorkVideoHlsJobCommand request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("No file uploaded.");
        }

        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var validationError = WorkVideoPolicy.ValidateVideoFile(request.File.FileName, request.File.ContentType, request.File.Length);
        if (validationError is not null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest(validationError);
        }

        var videos = (await commandStore.GetVideosForWorkAsync(request.WorkId, cancellationToken)).ToList();
        if (videos.Count >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var assetPublicUrls = await commandStore.GetAssetPublicUrlsAsync(
            WorkPublicThumbnailReadModel.GetThumbnailAssetIds(work.ThumbnailAssetId),
            cancellationToken);

        var storageType = storageSelector.ResolveStorageType();
        if (!storageSelector.TryGetStorage(storageType, out var storage))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("No video storage backend is available.");
        }

        var plan = WorkVideoHlsJobPlan.Create(
            request.WorkId,
            storageType,
            request.File.FileName,
            request.File.Length);

        await using (var workspace = await hlsWorkspace.CreateAsync(request.File, plan.VideoId, cancellationToken))
        {
            if (!await fileInspector.LooksLikeMp4Async(workspace.SourcePath, cancellationToken))
            {
                return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Only valid MP4 files are supported.");
            }

            var ffmpegError = await videoTranscoder.SegmentHlsAsync(
                workspace.SourcePath,
                workspace.HlsDirectory,
                WorkVideoPolicy.HlsManifestFileName,
                cancellationToken);
            if (ffmpegError is not null)
            {
                return WorkVideoResult<WorkVideosMutationResult>.BadRequest(ffmpegError);
            }

            await hlsOutputPublisher.PublishAsync(storage, workspace.HlsDirectory, plan.HlsPrefix, cancellationToken);
            var hasTimelinePreview = File.Exists(Path.Combine(workspace.HlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName))
                && File.Exists(Path.Combine(workspace.HlsDirectory, WorkVideoPolicy.TimelinePreviewSpriteFileName));

            var video = plan.ToWorkVideo(
                GetNextSortOrder(videos),
                DateTimeOffset.UtcNow,
                hasTimelinePreview);
            commandStore.AddWorkVideo(video);
            videos.Add(video);
            WorkPublicThumbnailReadModel.Refresh(
                work,
                WorkPublicThumbnailReadModel.ShouldLoadFallbackVideos(work.ThumbnailAssetId, assetPublicUrls)
                    ? videos
                    : Array.Empty<WorkVideo>(),
                assetPublicUrls);
            WorkPublicVideosReadModel.Refresh(work, videos);

            work.VideosVersion += 1;
            await commandStore.SaveChangesAsync(cancellationToken);
            return WorkVideoResult<WorkVideosMutationResult>.Ok(
                await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
        }
    }

    private static int GetNextSortOrder(IReadOnlyCollection<WorkVideo> videos)
    {
        return videos.Count == 0 ? 0 : videos.Max(video => video.SortOrder) + 1;
    }
}
