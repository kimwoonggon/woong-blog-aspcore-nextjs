using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Works.Support;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed class AddYouTubeWorkVideoCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<AddYouTubeWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        AddYouTubeWorkVideoCommand request,
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

        var videoId = WorkVideoPolicy.NormalizeYouTubeVideoId(request.YoutubeUrlOrId);
        if (videoId is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Provide a valid YouTube video URL or ID.");
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
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = videoId,
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
