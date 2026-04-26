using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Infrastructure.Modules.Content.Works.Persistence;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Storage;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class WorkVideoComponentTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    [Fact]
    public async Task LocalVideoStorageService_SaveReadAndDelete_UsesTempMediaRoot()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            var storage = CreateLocalStorage(tempRoot);
            var workId = Guid.NewGuid();
            var uploadSessionId = Guid.NewGuid();
            var storageKey = $"videos/{workId:N}/{uploadSessionId:N}.mp4";
            var physicalPath = Path.Combine(tempRoot, storageKey);

            await storage.SaveDirectUploadAsync(
                storageKey,
                new MemoryStream(SampleMp4Bytes),
                "video/mp4",
                CancellationToken.None);

            var target = await storage.CreateUploadTargetAsync(
                workId,
                uploadSessionId,
                storageKey,
                "video/mp4",
                CancellationToken.None);
            var storedObject = await storage.GetObjectAsync(storageKey, CancellationToken.None);
            var prefix = await storage.ReadPrefixAsync(storageKey, 12, CancellationToken.None);

            Assert.True(File.Exists(physicalPath));
            Assert.Equal("POST", target.UploadMethod);
            Assert.Equal($"/api/admin/works/{workId}/videos/upload?uploadSessionId={uploadSessionId}", target.UploadUrl);
            Assert.Equal(storageKey, target.StorageKey);
            Assert.Equal("video/mp4", storedObject?.ContentType);
            Assert.Equal((long)SampleMp4Bytes.Length, storedObject?.Size);
            Assert.Equal(SampleMp4Bytes[..12], prefix);

            await storage.DeleteAsync(storageKey, CancellationToken.None);

            Assert.False(File.Exists(physicalPath));
        }
        finally
        {
            DeleteDirectoryIfExists(tempRoot);
        }
    }

    [Fact]
    public async Task LocalVideoStorageService_DeleteManifest_RemovesHlsDirectory()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            var storage = CreateLocalStorage(tempRoot);
            var hlsPrefix = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}/hls";
            var hlsDirectory = Path.Combine(tempRoot, hlsPrefix);
            Directory.CreateDirectory(hlsDirectory);
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, WorkVideoPolicy.HlsManifestFileName), "#EXTM3U");
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, "segment_00000.ts"), "segment");
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName), "WEBVTT");

            await storage.DeleteAsync($"{hlsPrefix}/{WorkVideoPolicy.HlsManifestFileName}", CancellationToken.None);

            Assert.False(Directory.Exists(hlsDirectory));
        }
        finally
        {
            DeleteDirectoryIfExists(tempRoot);
        }
    }

    [Fact]
    public async Task WorkVideoCleanupService_ProcessCleanupJobs_DeletesLocalFileAndMarksJobSucceeded()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storage = CreateLocalStorage(tempRoot);
            var storageKey = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}.mp4";
            await storage.SaveDirectUploadAsync(storageKey, new MemoryStream(SampleMp4Bytes), "video/mp4", CancellationToken.None);
            var job = AddCleanupJob(dbContext, WorkVideoSourceTypes.Local, storageKey);
            await dbContext.SaveChangesAsync();

            var service = new WorkVideoService(new WorkVideoCleanupStore(dbContext), [storage]);
            var processedCount = await service.ProcessCleanupJobsAsync(CancellationToken.None);

            Assert.Equal(1, processedCount);
            Assert.Equal(VideoStorageCleanupJobStatuses.Succeeded, job.Status);
            Assert.Null(job.LastError);
            Assert.False(File.Exists(Path.Combine(tempRoot, storageKey)));
        }
        finally
        {
            DeleteDirectoryIfExists(tempRoot);
        }
    }

    [Fact]
    public async Task WorkVideoCleanupService_ProcessCleanupJobs_MarksMissingStorageAsFailed()
    {
        await using var dbContext = CreateDbContext();
        var job = AddCleanupJob(dbContext, WorkVideoSourceTypes.R2, $"videos/{Guid.NewGuid():N}/missing.mp4");
        await dbContext.SaveChangesAsync();

        var service = new WorkVideoService(new WorkVideoCleanupStore(dbContext), []);
        var processedCount = await service.ProcessCleanupJobsAsync(CancellationToken.None);

        Assert.Equal(1, processedCount);
        Assert.Equal(VideoStorageCleanupJobStatuses.Failed, job.Status);
        Assert.Equal("Storage backend not available.", job.LastError);
    }

    [Fact]
    public async Task WorkVideoCleanupService_ProcessCleanupJobs_RetriesFailedDeleteUntilMaxAttempts()
    {
        await using var dbContext = CreateDbContext();
        var job = AddCleanupJob(dbContext, WorkVideoSourceTypes.Local, $"videos/{Guid.NewGuid():N}/retry.mp4");
        job.AttemptCount = 4;
        await dbContext.SaveChangesAsync();

        var service = new WorkVideoService(
            new WorkVideoCleanupStore(dbContext),
            [new ThrowingVideoStorage(WorkVideoSourceTypes.Local, "delete failed")]);
        var processedCount = await service.ProcessCleanupJobsAsync(CancellationToken.None);

        Assert.Equal(1, processedCount);
        Assert.Equal(5, job.AttemptCount);
        Assert.Equal(VideoStorageCleanupJobStatuses.Failed, job.Status);
        Assert.Equal("delete failed", job.LastError);
    }

    [Fact]
    public async Task WorkVideoCleanupService_ExpireUploadSessions_ExpiresUnconfirmedSessionsAndEnqueuesCleanup()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var expiredIssued = AddUploadSession(
            dbContext,
            workId,
            WorkVideoUploadSessionStatuses.Issued,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            "videos/expired-issued.mp4");
        var expiredUploaded = AddUploadSession(
            dbContext,
            workId,
            WorkVideoUploadSessionStatuses.Uploaded,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            "videos/expired-uploaded.mp4");
        var confirmed = AddUploadSession(
            dbContext,
            workId,
            WorkVideoUploadSessionStatuses.Confirmed,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            "videos/confirmed.mp4");
        var active = AddUploadSession(
            dbContext,
            workId,
            WorkVideoUploadSessionStatuses.Issued,
            DateTimeOffset.UtcNow.AddMinutes(5),
            "videos/active.mp4");
        await dbContext.SaveChangesAsync();

        var service = new WorkVideoService(new WorkVideoCleanupStore(dbContext), []);
        var expiredCount = await service.ExpireUploadSessionsAsync(CancellationToken.None);

        Assert.Equal(2, expiredCount);
        Assert.Equal(WorkVideoUploadSessionStatuses.Expired, expiredIssued.Status);
        Assert.Equal(WorkVideoUploadSessionStatuses.Expired, expiredUploaded.Status);
        Assert.Equal(WorkVideoUploadSessionStatuses.Confirmed, confirmed.Status);
        Assert.Equal(WorkVideoUploadSessionStatuses.Issued, active.Status);
        Assert.Equal(
            new[] { "videos/expired-issued.mp4", "videos/expired-uploaded.mp4" },
            dbContext.VideoStorageCleanupJobs
                .OrderBy(job => job.StorageKey)
                .Select(job => job.StorageKey)
                .ToArray());
    }

    [Fact]
    public async Task ReorderWorkVideosCommandHandler_RewritesSortOrderDeterministically()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        dbContext.Works.Add(CreateWork(workId, 3, now));
        var first = AddVideo(dbContext, workId, "first", sortOrder: 20, now.AddSeconds(1));
        var second = AddVideo(dbContext, workId, "second", sortOrder: 10, now.AddSeconds(2));
        var third = AddVideo(dbContext, workId, "third", sortOrder: 10, now.AddSeconds(3));
        await dbContext.SaveChangesAsync();

        var handler = new ReorderWorkVideosCommandHandler(
            new WorkVideoCommandStore(dbContext),
            new WorkVideoQueryStore(dbContext, new TestPlaybackUrlBuilder()));
        var result = await handler.Handle(
            new ReorderWorkVideosCommand(workId, [third.Id, first.Id, second.Id], 3),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value?.VideosVersion);
        Assert.Equal(new[] { third.Id, first.Id, second.Id }, result.Value!.Videos.Select(video => video.Id).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, result.Value.Videos.Select(video => video.SortOrder).ToArray());

        var persisted = await dbContext.WorkVideos
            .Where(video => video.WorkId == workId)
            .OrderBy(video => video.SortOrder)
            .Select(video => new { video.Id, video.SortOrder })
            .ToListAsync();
        Assert.Equal(new[] { third.Id, first.Id, second.Id }, persisted.Select(video => video.Id).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, persisted.Select(video => video.SortOrder).ToArray());
    }

    [Fact]
    public async Task DeleteWorkVideoCommandHandler_QueuesCleanupAndCompactsRemainingSortOrder()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        dbContext.Works.Add(CreateWork(workId, 3, now));
        var first = AddVideo(dbContext, workId, "first", sortOrder: 0, now);
        var deleted = AddVideo(dbContext, workId, "deleted", sortOrder: 1, now.AddSeconds(1));
        var third = AddVideo(dbContext, workId, "third", sortOrder: 2, now.AddSeconds(2));
        deleted.SourceType = WorkVideoSourceTypes.Hls;
        deleted.SourceKey = WorkVideoHlsSourceKey.Create(WorkVideoSourceTypes.Local, $"videos/{workId:N}/{deleted.Id:N}/hls/master.m3u8");
        await dbContext.SaveChangesAsync();

        var handler = new DeleteWorkVideoCommandHandler(
            new WorkVideoCommandStore(dbContext),
            new WorkVideoCleanupStore(dbContext),
            new WorkVideoQueryStore(dbContext, new TestPlaybackUrlBuilder()));
        var result = await handler.Handle(new DeleteWorkVideoCommand(workId, deleted.Id, 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value?.VideosVersion);
        Assert.Equal(new[] { first.Id, third.Id }, result.Value!.Videos.Select(video => video.Id).ToArray());
        Assert.Equal(new[] { 0, 1 }, result.Value.Videos.Select(video => video.SortOrder).ToArray());
        Assert.False(await dbContext.WorkVideos.AnyAsync(video => video.Id == deleted.Id));

        var cleanupJob = await dbContext.VideoStorageCleanupJobs.SingleAsync();
        Assert.Equal(WorkVideoSourceTypes.Local, cleanupJob.StorageType);
        Assert.Equal($"videos/{workId:N}/{deleted.Id:N}/hls/master.m3u8", cleanupJob.StorageKey);
        Assert.Equal(VideoStorageCleanupJobStatuses.Pending, cleanupJob.Status);
    }

    private static LocalVideoStorageService CreateLocalStorage(string mediaRoot)
    {
        return new LocalVideoStorageService(Options.Create(new AuthOptions
        {
            MediaRoot = mediaRoot
        }));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"woong-blog-work-video-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static Work CreateWork(Guid workId, int videosVersion, DateTimeOffset now)
    {
        return new Work
        {
            Id = workId,
            Title = $"Work {workId:N}",
            Slug = $"work-{workId:N}",
            Excerpt = "work",
            Category = "video",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            VideosVersion = videosVersion,
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static WorkVideo AddVideo(
        WoongBlogDbContext dbContext,
        Guid workId,
        string sourceKey,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        var video = new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = sourceKey,
            OriginalFileName = sourceKey,
            SortOrder = sortOrder,
            CreatedAt = createdAt
        };
        dbContext.WorkVideos.Add(video);
        return video;
    }

    private static WorkVideoUploadSession AddUploadSession(
        WoongBlogDbContext dbContext,
        Guid workId,
        string status,
        DateTimeOffset expiresAt,
        string storageKey)
    {
        var session = new WorkVideoUploadSession
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            StorageType = WorkVideoSourceTypes.Local,
            StorageKey = storageKey,
            OriginalFileName = Path.GetFileName(storageKey),
            ExpectedMimeType = "video/mp4",
            ExpectedSize = 10,
            Status = status,
            ExpiresAt = expiresAt,
            CreatedAt = expiresAt.AddHours(-1)
        };
        dbContext.WorkVideoUploadSessions.Add(session);
        return session;
    }

    private static VideoStorageCleanupJob AddCleanupJob(
        WoongBlogDbContext dbContext,
        string storageType,
        string storageKey)
    {
        var job = new VideoStorageCleanupJob
        {
            Id = Guid.NewGuid(),
            StorageType = storageType,
            StorageKey = storageKey,
            Status = VideoStorageCleanupJobStatuses.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.VideoStorageCleanupJobs.Add(job);
        return job;
    }

    private sealed class TestPlaybackUrlBuilder : IWorkVideoPlaybackUrlBuilder
    {
        public string? BuildPlaybackUrl(string sourceType, string sourceKey)
        {
            return string.Equals(sourceType, WorkVideoSourceTypes.YouTube, StringComparison.OrdinalIgnoreCase)
                ? null
                : $"/media/{sourceKey}";
        }

        public string? BuildStorageObjectUrl(string storageType, string storageKey)
        {
            return $"/media/{storageKey}";
        }
    }

    private sealed class ThrowingVideoStorage(string storageType, string message) : IVideoObjectStorage
    {
        public string StorageType { get; } = storageType;

        public string? BuildPlaybackUrl(string storageKey)
        {
            return $"/media/{storageKey}";
        }

        public Task<VideoUploadTargetResult> CreateUploadTargetAsync(
            Guid workId,
            Guid uploadSessionId,
            string storageKey,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new VideoUploadTargetResult(uploadSessionId, "POST", "/upload", storageKey));
        }

        public Task SaveDirectUploadAsync(
            string storageKey,
            Stream stream,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<VideoStoredObject?> GetObjectAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<VideoStoredObject?>(new VideoStoredObject("video/mp4", 1));
        }

        public Task<byte[]> ReadPrefixAsync(string storageKey, int length, CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static readonly byte[] SampleMp4Bytes =
    [
        0x00, 0x00, 0x00, 0x18,
        0x66, 0x74, 0x79, 0x70,
        0x6D, 0x70, 0x34, 0x32,
        0x00, 0x00, 0x00, 0x00,
        0x6D, 0x70, 0x34, 0x32,
        0x69, 0x73, 0x6F, 0x6D
    ];
}
