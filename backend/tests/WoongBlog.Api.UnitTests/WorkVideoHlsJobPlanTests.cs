using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

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
        Assert.Equal(WorkVideoHlsSourceKey.Create(WorkVideoSourceTypes.Local, plan.ManifestStorageKey), plan.SourceKey);
        Assert.Equal(120, plan.OriginalFileName.Length);
        Assert.Equal(WorkVideoSourceTypes.Hls, video.SourceType);
        Assert.Equal(plan.SourceKey, video.SourceKey);
        Assert.Equal(WorkVideoPolicy.HlsManifestContentType, video.MimeType);
        Assert.Equal(1234, video.FileSize);
        Assert.Equal(3, video.SortOrder);
        Assert.Equal(DateTimeOffset.UnixEpoch, video.CreatedAt);
    }
}
