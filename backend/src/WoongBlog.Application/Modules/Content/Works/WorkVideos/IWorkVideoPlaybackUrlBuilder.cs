namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IWorkVideoPlaybackUrlBuilder
{
    string? BuildPlaybackUrl(string sourceType, string sourceKey);
    string? BuildStorageObjectUrl(string storageType, string storageKey);
}
