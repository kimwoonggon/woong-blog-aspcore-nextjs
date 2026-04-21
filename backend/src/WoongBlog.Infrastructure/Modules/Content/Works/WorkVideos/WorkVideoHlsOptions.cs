namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class WorkVideoHlsOptions
{
    public const string SectionName = "WorkVideos:Hls";

    public string FfmpegPath { get; set; } = "ffmpeg";
    public int SegmentDurationSeconds { get; set; } = 6;
    public int TimeoutSeconds { get; set; } = 300;
}
