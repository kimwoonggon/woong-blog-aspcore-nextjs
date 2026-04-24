namespace WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

public sealed class WorkVideoHlsOptions
{
    public const string SectionName = "WorkVideos:Hls";

    public string FfmpegPath { get; set; } = "ffmpeg";
    public string FfprobePath { get; set; } = "ffprobe";
    public int SegmentDurationSeconds { get; set; } = 6;
    public int TimelinePreviewIntervalSeconds { get; set; } = 5;
    public int TimelinePreviewTileColumns { get; set; } = 5;
    public int TimelinePreviewMaxFrames { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 300;
}
