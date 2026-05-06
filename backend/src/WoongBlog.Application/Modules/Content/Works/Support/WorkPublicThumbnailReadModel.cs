using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Application.Modules.Content.Works.Support;

public static class WorkPublicThumbnailReadModel
{
    public static void Refresh(
        Work work,
        IReadOnlyList<WorkVideo> videos,
        IReadOnlyDictionary<Guid, string> assetPublicUrls)
    {
        work.PublicThumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            work.ThumbnailAssetId,
            work.ContentJson,
            videos,
            assetPublicUrls);
    }

    public static Guid[] GetThumbnailAssetIds(Guid? thumbnailAssetId)
    {
        return thumbnailAssetId is Guid assetId ? [assetId] : [];
    }

    public static bool ShouldLoadFallbackVideos(
        Guid? thumbnailAssetId,
        IReadOnlyDictionary<Guid, string> assetPublicUrls)
    {
        return thumbnailAssetId is null || !assetPublicUrls.ContainsKey(thumbnailAssetId.Value);
    }
}
