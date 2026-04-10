namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoPlaybackUrlBuilder
{
    string? BuildPlaybackUrl(string sourceType, string sourceKey);
}
