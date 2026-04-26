using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Common.Application.Files;
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
    public void WorkVideoStorageSelector_UsesLocalInTestingUnlessR2IsForced()
    {
        var selector = CreateStorageSelector(
            "Testing",
            new CloudflareR2Options { ForceEnabledInDevelopment = false },
            [
                new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media"),
                new RecordingVideoStorage(WorkVideoSourceTypes.R2, "https://cdn.example.test/videos")
            ]);

        var storageType = selector.ResolveStorageType();

        Assert.Equal(WorkVideoSourceTypes.Local, storageType);
    }

    [Fact]
    public void WorkVideoStorageSelector_UsesR2WhenForcedInTestingAndPlaybackIsAvailable()
    {
        var selector = CreateStorageSelector(
            "Testing",
            new CloudflareR2Options { ForceEnabledInDevelopment = true },
            [
                new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media"),
                new RecordingVideoStorage(WorkVideoSourceTypes.R2, "https://cdn.example.test/videos")
            ]);

        var storageType = selector.ResolveStorageType();

        Assert.Equal(WorkVideoSourceTypes.R2, storageType);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void WorkVideoStorageSelector_FallsBackToLocalWhenR2IsMissingOrUnavailable(string environmentName)
    {
        var missingR2Selector = CreateStorageSelector(
            environmentName,
            new CloudflareR2Options(),
            [new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media")]);
        var unavailableR2Selector = CreateStorageSelector(
            environmentName,
            new CloudflareR2Options { ForceEnabledInDevelopment = true },
            [
                new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media"),
                new RecordingVideoStorage(WorkVideoSourceTypes.R2, playbackBaseUrl: null)
            ]);

        Assert.Equal(WorkVideoSourceTypes.Local, missingR2Selector.ResolveStorageType());
        Assert.Equal(WorkVideoSourceTypes.Local, unavailableR2Selector.ResolveStorageType());
    }

    [Fact]
    public void WorkVideoStorageSelector_TryGetStorage_IsCaseInsensitive()
    {
        var r2 = new RecordingVideoStorage(WorkVideoSourceTypes.R2, "https://cdn.example.test");
        var selector = CreateStorageSelector(
            "Production",
            new CloudflareR2Options(),
            [
                new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media"),
                r2
            ]);

        var found = selector.TryGetStorage("R2", out var storage);

        Assert.True(found);
        Assert.Same(r2, storage);
    }

    [Fact]
    public void WorkVideoPlaybackUrlBuilder_ResolvesDirectAndHlsStorageUrls()
    {
        var builder = new WorkVideoPlaybackUrlBuilder(
        [
            new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media/"),
            new RecordingVideoStorage(WorkVideoSourceTypes.R2, "https://cdn.example.test/")
        ]);
        var hlsManifestKey = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}/hls/{WorkVideoPolicy.HlsManifestFileName}";

        var localUrl = builder.BuildPlaybackUrl(WorkVideoSourceTypes.Local, "videos/local-video.mp4");
        var r2Url = builder.BuildPlaybackUrl("R2", "videos/r2-video.mp4");
        var hlsUrl = builder.BuildPlaybackUrl(
            WorkVideoSourceTypes.Hls,
            WorkVideoHlsSourceKey.Create(WorkVideoSourceTypes.R2, hlsManifestKey));

        Assert.Equal("/media/videos/local-video.mp4", localUrl);
        Assert.Equal("https://cdn.example.test/videos/r2-video.mp4", r2Url);
        Assert.Equal($"https://cdn.example.test/{hlsManifestKey}", hlsUrl);
        Assert.DoesNotContain("//videos", r2Url, StringComparison.Ordinal);
        Assert.DoesNotContain("//videos", hlsUrl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(WorkVideoSourceTypes.YouTube, "abc123")]
    [InlineData(WorkVideoSourceTypes.Hls, "not-a-valid-hls-source-key")]
    [InlineData("missing-storage", "videos/missing.mp4")]
    public void WorkVideoPlaybackUrlBuilder_ReturnsNullForUnsupportedSources(string sourceType, string sourceKey)
    {
        var builder = new WorkVideoPlaybackUrlBuilder(
        [
            new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media")
        ]);

        var url = builder.BuildPlaybackUrl(sourceType, sourceKey);

        Assert.Null(url);
    }

    [Fact]
    public void WorkVideoPlaybackUrlBuilder_BuildStorageObjectUrl_UsesRequestedStorage()
    {
        var builder = new WorkVideoPlaybackUrlBuilder(
        [
            new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media"),
            new RecordingVideoStorage(WorkVideoSourceTypes.R2, "https://cdn.example.test")
        ]);

        var vttUrl = builder.BuildStorageObjectUrl(
            WorkVideoSourceTypes.R2,
            $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}/hls/{WorkVideoPolicy.TimelinePreviewVttFileName}");
        var missingUrl = builder.BuildStorageObjectUrl("missing-storage", "videos/missing.vtt");

        Assert.EndsWith($"/{WorkVideoPolicy.TimelinePreviewVttFileName}", vttUrl, StringComparison.Ordinal);
        Assert.StartsWith("https://cdn.example.test/videos/", vttUrl, StringComparison.Ordinal);
        Assert.Null(missingUrl);
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
    public async Task LocalVideoStorageService_SaveOverwritesExistingFileAndDeleteIsIdempotent()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            var storage = CreateLocalStorage(tempRoot);
            var storageKey = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}.mp4";
            var replacementBytes = SampleMp4Bytes.Concat(new byte[] { 0x01, 0x02, 0x03 }).ToArray();

            await storage.SaveDirectUploadAsync(storageKey, new MemoryStream([0x00, 0x01]), "video/mp4", CancellationToken.None);
            await storage.SaveDirectUploadAsync(storageKey, new MemoryStream(replacementBytes), "video/mp4", CancellationToken.None);
            var storedObject = await storage.GetObjectAsync(storageKey, CancellationToken.None);
            var prefix = await storage.ReadPrefixAsync(storageKey, replacementBytes.Length + 10, CancellationToken.None);

            Assert.Equal(replacementBytes.Length, storedObject?.Size);
            Assert.Equal(replacementBytes, prefix);

            await storage.DeleteAsync(storageKey, CancellationToken.None);
            await storage.DeleteAsync(storageKey, CancellationToken.None);

            Assert.False(File.Exists(Path.Combine(tempRoot, storageKey)));
        }
        finally
        {
            DeleteDirectoryIfExists(tempRoot);
        }
    }

    [Fact]
    public async Task LocalVideoStorageService_MissingObjectOperationsAreSafe()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            var storage = CreateLocalStorage(tempRoot);
            var missingStorageKey = $"videos/{Guid.NewGuid():N}/missing.mp4";
            var missingManifestKey = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}/hls/{WorkVideoPolicy.HlsManifestFileName}";

            var storedObject = await storage.GetObjectAsync(missingStorageKey, CancellationToken.None);
            var prefix = await storage.ReadPrefixAsync(missingStorageKey, 64, CancellationToken.None);
            await storage.DeleteAsync(missingStorageKey, CancellationToken.None);
            await storage.DeleteAsync(missingManifestKey, CancellationToken.None);
            await storage.DeleteAsync(missingManifestKey, CancellationToken.None);

            Assert.Null(storedObject);
            Assert.Empty(prefix);
            Assert.False(File.Exists(Path.Combine(tempRoot, missingStorageKey)));
            Assert.False(Directory.Exists(Path.GetDirectoryName(Path.Combine(tempRoot, missingManifestKey))));
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
    public async Task WorkVideoHlsWorkspace_CreatesSeparatedSourceAndOutputPathsAndCleansUpLease()
    {
        var videoId = Guid.NewGuid();
        var expectedTempDirectory = Path.Combine(Path.GetTempPath(), "woong-blog-hls", videoId.ToString("N"));
        DeleteDirectoryIfExists(expectedTempDirectory);
        var workspaceFactory = new WorkVideoHlsWorkspace();

        try
        {
            await using var workspace = await workspaceFactory.CreateAsync(
                new TestUploadedFile("source.mp4", "video/mp4", SampleMp4Bytes),
                videoId,
                CancellationToken.None);

            Assert.Equal(expectedTempDirectory, workspace.TempDirectory);
            Assert.Equal(Path.Combine(expectedTempDirectory, "source.mp4"), workspace.SourcePath);
            Assert.Equal(Path.Combine(expectedTempDirectory, "hls"), workspace.HlsDirectory);
            Assert.True(File.Exists(workspace.SourcePath));
            Assert.True(Directory.Exists(workspace.HlsDirectory));
            Assert.NotEqual(Path.GetDirectoryName(workspace.SourcePath), workspace.HlsDirectory);
            Assert.Equal(SampleMp4Bytes, await File.ReadAllBytesAsync(workspace.SourcePath));
        }
        finally
        {
            DeleteDirectoryIfExists(expectedTempDirectory);
        }

        Assert.False(Directory.Exists(expectedTempDirectory));
    }

    [Fact]
    public async Task WorkVideoHlsOutputPublisher_StoresArtifactsWithExpectedContentTypesAndKeys()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            var hlsDirectory = Path.Combine(tempRoot, "hls-output");
            Directory.CreateDirectory(hlsDirectory);
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, "segment_00001.ts"), "segment");
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewSpriteFileName), "sprite");
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, WorkVideoPolicy.HlsManifestFileName), "#EXTM3U");
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName), "WEBVTT");
            var hlsPrefix = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}/hls";
            var storage = new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media");

            await new WorkVideoHlsOutputPublisher().PublishAsync(storage, hlsDirectory, hlsPrefix, CancellationToken.None);

            Assert.Equal(
            [
                $"{hlsPrefix}/{WorkVideoPolicy.HlsManifestFileName}",
                $"{hlsPrefix}/segment_00001.ts",
                $"{hlsPrefix}/{WorkVideoPolicy.TimelinePreviewSpriteFileName}",
                $"{hlsPrefix}/{WorkVideoPolicy.TimelinePreviewVttFileName}"
            ], storage.SavedObjects.Select(saved => saved.StorageKey).ToArray());
            Assert.Equal(WorkVideoPolicy.HlsManifestContentType, storage.SavedObjects[0].ContentType);
            Assert.Equal(WorkVideoPolicy.HlsSegmentContentType, storage.SavedObjects[1].ContentType);
            Assert.Equal(WorkVideoPolicy.TimelinePreviewSpriteContentType, storage.SavedObjects[2].ContentType);
            Assert.Equal(WorkVideoPolicy.TimelinePreviewVttContentType, storage.SavedObjects[3].ContentType);
        }
        finally
        {
            DeleteDirectoryIfExists(tempRoot);
        }
    }

    [Fact]
    public async Task WorkVideoHlsOutputPublisher_StopsOnStorageFailureAndLeavesPreviouslySavedArtifactsVisible()
    {
        var tempRoot = CreateTempDirectory();

        try
        {
            var hlsDirectory = Path.Combine(tempRoot, "hls-output");
            Directory.CreateDirectory(hlsDirectory);
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, WorkVideoPolicy.HlsManifestFileName), "#EXTM3U");
            await File.WriteAllTextAsync(Path.Combine(hlsDirectory, "segment_00001.ts"), "segment");
            var hlsPrefix = $"videos/{Guid.NewGuid():N}/{Guid.NewGuid():N}/hls";
            var storage = new RecordingVideoStorage(WorkVideoSourceTypes.Local, "/media")
            {
                FailOnStorageKey = $"{hlsPrefix}/segment_00001.ts"
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new WorkVideoHlsOutputPublisher().PublishAsync(storage, hlsDirectory, hlsPrefix, CancellationToken.None));

            Assert.Equal("storage save failed", exception.Message);
            Assert.Single(storage.SavedObjects);
            Assert.Equal($"{hlsPrefix}/{WorkVideoPolicy.HlsManifestFileName}", storage.SavedObjects[0].StorageKey);
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

    [Fact]
    public async Task WorkVideoCleanupStore_EnqueuesHlsManifestCleanupOnceForUnderlyingStorage()
    {
        await using var dbContext = CreateDbContext();
        var store = new WorkVideoCleanupStore(dbContext);
        var workId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var manifestStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.HlsManifestFileName}";
        var hlsSourceKey = WorkVideoHlsSourceKey.Create(WorkVideoSourceTypes.R2, manifestStorageKey);
        var now = new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

        await store.EnqueueCleanupAsync(workId, videoId, WorkVideoSourceTypes.Hls, hlsSourceKey, now, CancellationToken.None);
        await store.SaveChangesAsync(CancellationToken.None);
        await store.EnqueueCleanupAsync(workId, videoId, WorkVideoSourceTypes.Hls, hlsSourceKey, now.AddMinutes(1), CancellationToken.None);
        await store.SaveChangesAsync(CancellationToken.None);

        var cleanupJob = await dbContext.VideoStorageCleanupJobs.SingleAsync();
        Assert.Equal(workId, cleanupJob.WorkId);
        Assert.Equal(videoId, cleanupJob.WorkVideoId);
        Assert.Equal(WorkVideoSourceTypes.R2, cleanupJob.StorageType);
        Assert.Equal(manifestStorageKey, cleanupJob.StorageKey);
        Assert.Equal(VideoStorageCleanupJobStatuses.Pending, cleanupJob.Status);
        Assert.Equal(now, cleanupJob.CreatedAt);
    }

    [Fact]
    public async Task WorkVideoCleanupStore_SkipsYouTubeAndMalformedHlsSourceKeys()
    {
        await using var dbContext = CreateDbContext();
        var store = new WorkVideoCleanupStore(dbContext);
        var now = new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

        await store.EnqueueCleanupAsync(Guid.NewGuid(), Guid.NewGuid(), WorkVideoSourceTypes.YouTube, "youtube-id", now, CancellationToken.None);
        await store.EnqueueCleanupAsync(Guid.NewGuid(), Guid.NewGuid(), WorkVideoSourceTypes.Hls, "malformed-hls-source-key", now, CancellationToken.None);
        await store.SaveChangesAsync(CancellationToken.None);

        Assert.Empty(dbContext.VideoStorageCleanupJobs);
    }

    private static LocalVideoStorageService CreateLocalStorage(string mediaRoot)
    {
        return new LocalVideoStorageService(Options.Create(new AuthOptions
        {
            MediaRoot = mediaRoot
        }));
    }

    private static WorkVideoStorageSelector CreateStorageSelector(
        string environmentName,
        CloudflareR2Options r2Options,
        IReadOnlyList<IVideoObjectStorage> storages)
    {
        return new WorkVideoStorageSelector(
            storages,
            new TestHostEnvironment(environmentName),
            Options.Create(r2Options));
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

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "WoongBlog.Api.ComponentTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestUploadedFile(string fileName, string contentType, byte[] content) : IUploadedFile
    {
        public string FileName { get; } = fileName;
        public string ContentType { get; } = contentType;
        public long Length => content.Length;

        public Stream OpenReadStream()
        {
            return new MemoryStream(content);
        }
    }

    private sealed record SavedVideoObject(string StorageKey, string ContentType, byte[] Bytes);

    private sealed class RecordingVideoStorage(string storageType, string? playbackBaseUrl) : IVideoObjectStorage
    {
        public string StorageType { get; } = storageType;
        public string? FailOnStorageKey { get; init; }
        public List<SavedVideoObject> SavedObjects { get; } = [];

        public string? BuildPlaybackUrl(string storageKey)
        {
            return playbackBaseUrl is null
                ? null
                : $"{playbackBaseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";
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

        public async Task SaveDirectUploadAsync(
            string storageKey,
            Stream stream,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (string.Equals(storageKey, FailOnStorageKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("storage save failed");
            }

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            SavedObjects.Add(new SavedVideoObject(storageKey, contentType, memory.ToArray()));
        }

        public Task<VideoStoredObject?> GetObjectAsync(string storageKey, CancellationToken cancellationToken)
        {
            var savedObject = SavedObjects.SingleOrDefault(saved => string.Equals(saved.StorageKey, storageKey, StringComparison.Ordinal));
            return Task.FromResult(savedObject is null
                ? null
                : new VideoStoredObject(savedObject.ContentType, savedObject.Bytes.Length));
        }

        public Task<byte[]> ReadPrefixAsync(string storageKey, int length, CancellationToken cancellationToken)
        {
            var savedObject = SavedObjects.SingleOrDefault(saved => string.Equals(saved.StorageKey, storageKey, StringComparison.Ordinal));
            return Task.FromResult(savedObject is null ? [] : savedObject.Bytes[..Math.Min(savedObject.Bytes.Length, length)]);
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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
