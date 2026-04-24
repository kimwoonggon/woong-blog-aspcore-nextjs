using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Unit)]
public sealed class WorkVideoHlsJobPlanTests
{
    [Fact]
    public void Create_BuildsHlsStoragePathsAndSanitizedVideoEntity()
    {
        var workId = Guid.NewGuid();
        var longFileName = $"{new string('v', 140)}.mp4";

        var plan = WorkVideoHlsJobPlan.Create(workId, WorkVideoSourceTypes.Local, longFileName, 1234);
        var video = plan.ToWorkVideo(sortOrder: 3, createdAt: DateTimeOffset.UnixEpoch);

        Assert.Equal(workId, plan.WorkId);
        Assert.Equal($"videos/{workId:N}/{plan.VideoId:N}/hls", plan.HlsPrefix);
        Assert.Equal($"{plan.HlsPrefix}/{WorkVideoPolicy.HlsManifestFileName}", plan.ManifestStorageKey);
        Assert.Equal($"{plan.HlsPrefix}/{WorkVideoPolicy.TimelinePreviewVttFileName}", plan.TimelinePreviewVttStorageKey);
        Assert.Equal($"{plan.HlsPrefix}/{WorkVideoPolicy.TimelinePreviewSpriteFileName}", plan.TimelinePreviewSpriteStorageKey);
        Assert.Equal(WorkVideoHlsSourceKey.Create(WorkVideoSourceTypes.Local, plan.ManifestStorageKey), plan.SourceKey);
        Assert.Equal(120, plan.OriginalFileName.Length);
        Assert.Equal(WorkVideoSourceTypes.Hls, video.SourceType);
        Assert.Equal(plan.SourceKey, video.SourceKey);
        Assert.Equal(WorkVideoPolicy.HlsManifestContentType, video.MimeType);
        Assert.Equal(1234, video.FileSize);
        Assert.Equal(plan.TimelinePreviewVttStorageKey, video.TimelinePreviewVttStorageKey);
        Assert.Equal(plan.TimelinePreviewSpriteStorageKey, video.TimelinePreviewSpriteStorageKey);
        Assert.Equal(3, video.SortOrder);
        Assert.Equal(DateTimeOffset.UnixEpoch, video.CreatedAt);
    }

    [Fact]
    public void ToWorkVideo_OmitsTimelinePreviewKeys_WhenPreviewWasNotGenerated()
    {
        var plan = WorkVideoHlsJobPlan.Create(Guid.NewGuid(), WorkVideoSourceTypes.Local, "short.mp4", 42);

        var video = plan.ToWorkVideo(sortOrder: 0, createdAt: DateTimeOffset.UnixEpoch, includeTimelinePreview: false);

        Assert.Null(video.TimelinePreviewVttStorageKey);
        Assert.Null(video.TimelinePreviewSpriteStorageKey);
    }
}
