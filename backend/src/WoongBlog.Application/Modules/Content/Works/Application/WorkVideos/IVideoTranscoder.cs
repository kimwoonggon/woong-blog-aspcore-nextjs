namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IVideoTranscoder
{
    Task<string?> SegmentHlsAsync(
        string inputPath,
        string hlsDirectory,
        string manifestFileName,
        CancellationToken cancellationToken);
}
