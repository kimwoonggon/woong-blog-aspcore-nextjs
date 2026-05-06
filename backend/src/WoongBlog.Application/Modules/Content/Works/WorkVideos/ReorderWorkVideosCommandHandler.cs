using MediatR;
using WoongBlog.Application.Modules.Content.Works.Support;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed class ReorderWorkVideosCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<ReorderWorkVideosCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        ReorderWorkVideosCommand request,
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

        var videos = await commandStore.GetVideosForWorkAsync(request.WorkId, cancellationToken);
        if (videos.Count != request.OrderedVideoIds.Count
            || videos.Any(video => !request.OrderedVideoIds.Contains(video.Id)))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Reorder payload must include every video exactly once.");
        }

        var assetPublicUrls = await commandStore.GetAssetPublicUrlsAsync(
            WorkPublicThumbnailReadModel.GetThumbnailAssetIds(work.ThumbnailAssetId),
            cancellationToken);

        for (var index = 0; index < videos.Count; index += 1)
        {
            videos[index].SortOrder = request.OrderedVideoIds.Count + index;
        }

        await commandStore.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < request.OrderedVideoIds.Count; index += 1)
        {
            videos.Single(video => video.Id == request.OrderedVideoIds[index]).SortOrder = index;
        }

        WorkPublicThumbnailReadModel.Refresh(work, videos, assetPublicUrls);
        WorkPublicVideosReadModel.Refresh(work, videos);
        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}
