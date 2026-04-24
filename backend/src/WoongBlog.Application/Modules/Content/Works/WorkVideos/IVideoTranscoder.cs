namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IVideoTranscoder
{
    Task<string?> SegmentHlsAsync(
        string inputPath,
        string hlsDirectory,
        string manifestFileName,
        CancellationToken cancellationToken);
}
