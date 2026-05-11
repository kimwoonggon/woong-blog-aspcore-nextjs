using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Support;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Persistence.Diagnostics;
using WoongBlog.Infrastructure.Modules.Composition.Persistence;
using WoongBlog.Infrastructure.Modules.Content.Blogs.Persistence;
using WoongBlog.Infrastructure.Modules.Content.Works.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public sealed class PostgresPersistenceContractTests : IClassFixture<PostgresPersistenceContractTests.PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresPersistenceContractTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Bootstrapper_AppliesPostgresSpecificSearchSchema()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Blogs' AND column_name = 'SearchTitle')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'SearchText')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Blogs' AND column_name = 'PublicContentHtml')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'PublicContentMarkdown')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'PublicThumbnailUrl')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'PublicIconUrl')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'PublicSocialShareMessage')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'PublicVideosJson')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Blogs' AND column_name = 'PublicCoverUrl')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Blogs_SearchTitle_Trgm')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Works_SearchText_Trgm')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Blogs_PublicList_Covering')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Works_PublicList_Covering')",
            cancellationToken));
        var worksPublicListIndexDefinition = await ScalarStringAsync(
            connection,
            "SELECT indexdef FROM pg_indexes WHERE indexname = 'IX_Works_PublicList_Covering'",
            cancellationToken);
        Assert.Contains("\"PublicThumbnailUrl\"", worksPublicListIndexDefinition, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Period\"", worksPublicListIndexDefinition, StringComparison.Ordinal);
        Assert.DoesNotContain("\"PublicIconUrl\"", worksPublicListIndexDefinition, StringComparison.Ordinal);
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260419_content_search_fields')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_public_content_body_fields')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_public_media_url_fields')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_public_list_covering_indexes')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260511_public_work_list_covering_index_visible_fields')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_public_work_social_share_message')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_work_video_version_backfill')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260507_public_work_videos_read_model')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_public_work_thumbnail_fallback_backfill')",
            cancellationToken));
    }

    [Fact]
    public async Task Bootstrapper_RebuildsLegacyWideWorkPublicListCoveringIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DROP INDEX IF EXISTS "IX_Works_PublicList_Covering";

            CREATE INDEX "IX_Works_PublicList_Covering"
            ON "Works" ("Published", "PublishedAt" DESC)
            INCLUDE ("Id", "Slug", "Title", "Excerpt", "Category", "Period", "Tags", "PublicThumbnailUrl", "PublicIconUrl");

            DELETE FROM "SchemaPatches"
            WHERE "Id" = '20260511_public_work_list_covering_index_visible_fields';
            """,
            cancellationToken);
        dbContext.ChangeTracker.Clear();

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        var worksPublicListIndexDefinition = await ScalarStringAsync(
            connection,
            "SELECT indexdef FROM pg_indexes WHERE indexname = 'IX_Works_PublicList_Covering'",
            cancellationToken);
        Assert.Contains("\"PublicThumbnailUrl\"", worksPublicListIndexDefinition, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Period\"", worksPublicListIndexDefinition, StringComparison.Ordinal);
        Assert.DoesNotContain("\"PublicIconUrl\"", worksPublicListIndexDefinition, StringComparison.Ordinal);
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260511_public_work_list_covering_index_visible_fields')",
            cancellationToken));
    }

    [Fact]
    public async Task Bootstrapper_BackfillsVideosVersion_ForExistingWorkVideos()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var work = CreatePublishedWork(
            "legacy-work-video-version",
            "Legacy Work Video Version",
            publishedAtOffsetMinutes: -1);
        work.VideosVersion = 0;
        dbContext.Works.Add(work);
        dbContext.WorkVideos.Add(new WorkVideo
        {
            WorkId = work.Id,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = "legacy-video-version",
            OriginalFileName = "Legacy Video Version",
            SortOrder = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        Assert.Equal(
            1,
            await dbContext.Works
                .Where(x => x.Id == work.Id)
                .Select(x => x.VideosVersion)
                .SingleAsync(cancellationToken));

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_work_video_version_backfill')",
            cancellationToken));
    }

    [Fact]
    public async Task Bootstrapper_BackfillsPublicVideosJson_ForExistingWorkVideos()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var work = CreatePublishedWork(
            "legacy-public-video-read-model",
            "Legacy Public Video Read Model",
            publishedAtOffsetMinutes: -1);
        work.PublicVideosJson = "[]";
        var videoId = Guid.NewGuid();
        dbContext.Works.Add(work);
        dbContext.WorkVideos.Add(new WorkVideo
        {
            Id = videoId,
            WorkId = work.Id,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = "legacy-public-video",
            OriginalFileName = "Legacy Public Video",
            SortOrder = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var publicVideosJson = await dbContext.Works
            .Where(x => x.Id == work.Id)
            .Select(x => x.PublicVideosJson)
            .SingleAsync(cancellationToken);
        var publicVideo = Assert.Single(WorkPublicVideosReadModel.Deserialize(publicVideosJson));
        Assert.Equal(videoId, publicVideo.Id);
        Assert.Equal(WorkVideoSourceTypes.YouTube, publicVideo.SourceType);
        Assert.Equal("legacy-public-video", publicVideo.SourceKey);
        Assert.DoesNotContain("originalFileName", publicVideosJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fileSize", publicVideosJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", publicVideosJson, StringComparison.OrdinalIgnoreCase);

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260507_public_work_videos_read_model')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260507_public_work_videos_public_dto')",
            cancellationToken));
    }

    [Fact]
    public async Task Bootstrapper_BackfillsPublicThumbnailUrl_FromLegacyFallbacks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var youtubeWork = CreatePublishedWork(
            "legacy-youtube-thumbnail-fallback",
            "Legacy YouTube Thumbnail Fallback",
            publishedAtOffsetMinutes: -1);
        youtubeWork.ContentJson = """{"html":"<p>youtube fallback</p>"}""";
        youtubeWork.PublicThumbnailUrl = string.Empty;
        var bodyWork = CreatePublishedWork(
            "legacy-body-thumbnail-fallback",
            "Legacy Body Thumbnail Fallback",
            publishedAtOffsetMinutes: -2);
        bodyWork.ContentJson = """{"html":"<p>body fallback</p><img src=\"/media/legacy-body-fallback.png\" alt=\"body\">"}""";
        bodyWork.PublicThumbnailUrl = string.Empty;
        var localVideoWork = CreatePublishedWork(
            "legacy-local-video-thumbnail-fallback",
            "Legacy Local Video Thumbnail Fallback",
            publishedAtOffsetMinutes: -3);
        localVideoWork.ContentJson = """{"html":"<p>local video fallback</p><img src=\"/media/should-not-win.png\" alt=\"body\">"}""";
        localVideoWork.PublicThumbnailUrl = string.Empty;

        dbContext.Works.AddRange(youtubeWork, bodyWork, localVideoWork);
        dbContext.WorkVideos.AddRange(
            new WorkVideo
            {
                WorkId = youtubeWork.Id,
                SourceType = WorkVideoSourceTypes.YouTube,
                SourceKey = "dQw4w9WgXcQ",
                OriginalFileName = "Legacy YouTube",
                SortOrder = 0,
                CreatedAt = now
            },
            new WorkVideo
            {
                WorkId = localVideoWork.Id,
                SourceType = WorkVideoSourceTypes.Local,
                SourceKey = "videos/local-fallback.mp4",
                OriginalFileName = "Legacy Local",
                SortOrder = 0,
                CreatedAt = now
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        Assert.Equal(
            "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg",
            await dbContext.Works
                .Where(x => x.Id == youtubeWork.Id)
                .Select(x => x.PublicThumbnailUrl)
                .SingleAsync(cancellationToken));
        Assert.Equal(
            "/media/legacy-body-fallback.png",
            await dbContext.Works
                .Where(x => x.Id == bodyWork.Id)
                .Select(x => x.PublicThumbnailUrl)
                .SingleAsync(cancellationToken));
        Assert.Equal(
            string.Empty,
            await dbContext.Works
                .Where(x => x.Id == localVideoWork.Id)
                .Select(x => x.PublicThumbnailUrl)
                .SingleAsync(cancellationToken));

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260506_public_work_thumbnail_fallback_backfill')",
            cancellationToken));
    }

    [Fact]
    public async Task Bootstrapper_IsIdempotentAndPreservesRuntimeData_WithPostgres()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);
        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var countsBeforeRuntimeData = await CountSeededDataAsync(dbContext, cancellationToken);
        dbContext.Blogs.Add(CreateBlog("runtime-preserved-blog", "Runtime Preserved Blog"));
        await dbContext.SaveChangesAsync(cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);
        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var countsAfterRepeatedBootstrap = await CountSeededDataAsync(dbContext, cancellationToken);
        Assert.Equal(countsBeforeRuntimeData.Blogs + 1, countsAfterRepeatedBootstrap.Blogs);
        Assert.Equal(countsBeforeRuntimeData.Works, countsAfterRepeatedBootstrap.Works);
        Assert.Equal(countsBeforeRuntimeData.Pages, countsAfterRepeatedBootstrap.Pages);
        Assert.Equal(countsBeforeRuntimeData.SiteSettings, countsAfterRepeatedBootstrap.SiteSettings);
        Assert.Equal(countsBeforeRuntimeData.SchemaPatches, countsAfterRepeatedBootstrap.SchemaPatches);
        Assert.Equal(1, await dbContext.Blogs.CountAsync(blog => blog.Slug == "runtime-preserved-blog", cancellationToken));
    }

    [Fact]
    public async Task RelationalConstraints_EnforceRequiredUniqueAndCascadeContracts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var dbContext = CreateDbContext())
        {
            await ResetDatabaseAsync(dbContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);
        }

        await using (var dbContext = CreateDbContext())
        {
            dbContext.Blogs.Add(new Blog
            {
                Slug = "required-title-blog",
                Title = null!,
                Excerpt = "required title",
                ContentJson = "{}"
            });

            await AssertPostgresConstraintViolationAsync(
                PostgresErrorCodes.NotNullViolation,
                () => dbContext.SaveChangesAsync(cancellationToken));
        }

        await using (var dbContext = CreateDbContext())
        {
            dbContext.Blogs.AddRange(
                CreateBlog("duplicate-blog-slug", "Duplicate Blog One"),
                CreateBlog("duplicate-blog-slug", "Duplicate Blog Two"));

            await AssertPostgresConstraintViolationAsync(
                PostgresErrorCodes.UniqueViolation,
                () => dbContext.SaveChangesAsync(cancellationToken));
        }

        await using (var dbContext = CreateDbContext())
        {
            dbContext.AuthSessions.AddRange(
                new AuthSession
                {
                    ProfileId = Guid.NewGuid(),
                    SessionKey = "duplicate-session-key",
                    UserAgent = "Test",
                    IpAddress = "127.0.0.1"
                },
                new AuthSession
                {
                    ProfileId = Guid.NewGuid(),
                    SessionKey = "duplicate-session-key",
                    UserAgent = "Test",
                    IpAddress = "127.0.0.1"
                });

            await AssertPostgresConstraintViolationAsync(
                PostgresErrorCodes.UniqueViolation,
                () => dbContext.SaveChangesAsync(cancellationToken));
        }

        await using (var dbContext = CreateDbContext())
        {
            var work = CreateWork("cascade-work");
            dbContext.Works.Add(work);
            dbContext.WorkVideos.AddRange(
                new WorkVideo
                {
                    WorkId = work.Id,
                    SourceType = "local",
                    SourceKey = "videos/cascade-0.mp4",
                    OriginalFileName = "Cascade 0.mp4",
                    SortOrder = 0
                },
                new WorkVideo
                {
                    WorkId = work.Id,
                    SourceType = "local",
                    SourceKey = "videos/cascade-1.mp4",
                    OriginalFileName = "Cascade 1.mp4",
                    SortOrder = 1
                });
            dbContext.WorkVideoUploadSessions.Add(new WorkVideoUploadSession
            {
                WorkId = work.Id,
                StorageType = "local",
                StorageKey = "uploads/cascade.mp4",
                OriginalFileName = "Cascade.mp4",
                ExpectedMimeType = "video/mp4",
                ExpectedSize = 1024,
                Status = WorkVideoUploadSessionStatuses.Issued,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.ChangeTracker.Clear();
            var reloadedWork = await dbContext.Works.SingleAsync(candidate => candidate.Id == work.Id, cancellationToken);
            dbContext.Works.Remove(reloadedWork);
            await dbContext.SaveChangesAsync(cancellationToken);

            Assert.False(await dbContext.WorkVideos.AnyAsync(video => video.WorkId == work.Id, cancellationToken));
            Assert.False(await dbContext.WorkVideoUploadSessions.AnyAsync(session => session.WorkId == work.Id, cancellationToken));
        }
    }

    [Fact]
    public async Task CommandDiagnosticsInterceptor_RecordsAsyncEfCommands_WithPostgres()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            setupContext.Blogs.Add(CreateBlog("async-command-diagnostics-blog", "Async Command Diagnostics Blog"));
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var collector = CreateDiagnosticsCollector();
        await using var dbContext = _fixture.CreateDbContext(new LoadTestDbCommandDiagnosticsInterceptor(collector));

        var before = collector.CaptureSnapshot();
        Assert.Equal(0, before.CommandLatency.SampleCount);

        var exists = await dbContext.Blogs
            .AsNoTracking()
            .AnyAsync(blog => blog.Slug == "async-command-diagnostics-blog", cancellationToken);
        var updatedRows = await dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE "Blogs"
            SET "UpdatedAt" = "UpdatedAt"
            WHERE "Slug" = 'async-command-diagnostics-blog'
            """,
            cancellationToken);

        var snapshot = collector.CaptureSnapshot();
        Assert.True(exists);
        Assert.Equal(1, updatedRows);
        Assert.True(snapshot.CommandLatency.SampleCount >= 2);
        Assert.NotNull(snapshot.CommandLatency.P95Ms);
    }

    [Fact]
    public async Task PublicWorkDetail_UsesStoredSocialShareMessage_WithPostgres()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);
        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var workId = Guid.NewGuid();
        const string slug = "postgres-public-social-share";
        dbContext.Works.Add(new Work
        {
            Id = workId,
            Slug = slug,
            Title = "Postgres Public Social Share",
            Excerpt = "Public detail",
            Category = "case-study",
            ContentJson = """{"html":"<p>public body</p>"}""",
            AllPropertiesJson = """{"socialShareMessage":"Stale admin JSON message"}""",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "Works"
            SET "PublicSocialShareMessage" = {"Stored public message"}
            WHERE "Id" = {workId}
            """,
            cancellationToken);
        dbContext.ChangeTracker.Clear();

        var queryStore = new WorkQueryStore(dbContext, new NoopPlaybackUrlBuilder());

        var result = await queryStore.GetPublishedDetailBySlugAsync(slug, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Stored public message", result!.SocialShareMessage);
    }

    [Fact]
    public async Task PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand_AndResolverEquivalentStoredThumbnail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string slug = "postgres-public-work-no-videos";
        const string expectedThumbnailUrl = "/media/postgres-public-work-no-videos-thumb.png";
        const string contentJson = """{"html":"<p><img src=\"/media/body-fallback-should-not-win.png\" alt=\"body\"></p>"}""";
        var thumbnailAssetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Assets.Add(new Asset
            {
                Id = thumbnailAssetId,
                Bucket = "media",
                Path = "postgres-public-work-no-videos-thumb.png",
                PublicUrl = expectedThumbnailUrl,
                MimeType = "image/png",
                Kind = "work-thumbnail",
                CreatedAt = now
            });
            setupContext.Works.Add(new Work
            {
                Slug = slug,
                Title = "Postgres Public Work No Videos",
                Excerpt = "Postgres Public Work No Videos excerpt",
                Category = "case-study",
                ContentJson = contentJson,
                PublicContentHtml = "<p>Postgres Public Work No Videos</p>",
                AllPropertiesJson = "{}",
                ThumbnailAssetId = thumbnailAssetId,
                PublicThumbnailUrl = expectedThumbnailUrl,
                Published = true,
                PublishedAt = now.AddMinutes(-1),
                CreatedAt = now,
                UpdatedAt = now
            });
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var resolverThumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            thumbnailAssetId,
            contentJson,
            Array.Empty<WorkVideo>(),
            new Dictionary<Guid, string> { [thumbnailAssetId] = expectedThumbnailUrl });
        var collector = CreateDiagnosticsCollector();
        await using var dbContext = _fixture.CreateDbContext(new LoadTestDbCommandDiagnosticsInterceptor(collector));
        var queryStore = new WorkQueryStore(dbContext, new NoopPlaybackUrlBuilder());

        var result = await queryStore.GetPublishedDetailBySlugAsync(slug, cancellationToken);
        var snapshot = collector.CaptureSnapshot();

        Assert.NotNull(result);
        Assert.Equal(resolverThumbnailUrl, result!.ThumbnailUrl);
        Assert.Empty(result!.Videos);
        Assert.Equal(1, snapshot.CommandLatency.SampleCount);
    }

    [Fact]
    public async Task PublicWorkDetailWithoutVideos_DoesNotReferenceWorkVideosInDetailProjection()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string slug = "postgres-public-work-no-video-exists";
        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Works.Add(CreatePublishedWork(slug, "Postgres Public Work No Video Exists", publishedAtOffsetMinutes: -1));
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var commandTextCapture = new CommandTextCaptureInterceptor();
        await using var dbContext = _fixture.CreateDbContext(commandTextCapture);
        var queryStore = new WorkQueryStore(dbContext, new NoopPlaybackUrlBuilder());

        var result = await queryStore.GetPublishedDetailBySlugAsync(slug, cancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result!.Videos);
        var commandText = Assert.Single(commandTextCapture.CommandTexts);
        Assert.DoesNotContain("\"WorkVideos\"", commandText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicWorkDetailWithVideos_UsesSinglePostgresCommand_AndStoredPublicColumnsOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string slug = "postgres-public-work-with-videos";
        const string expectedThumbnailUrl = "/media/postgres-public-work-with-videos-thumb.png";
        const string contentJson = """{"html":"<p><img src=\"/media/body-fallback-should-not-win.png\" alt=\"body\"></p>"}""";
        var workId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var thumbnailAssetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdAt = now.AddSeconds(-1);

        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Assets.Add(new Asset
            {
                Id = thumbnailAssetId,
                Bucket = "media",
                Path = "postgres-public-work-with-videos-thumb.png",
                PublicUrl = expectedThumbnailUrl,
                MimeType = "image/png",
                Kind = "work-thumbnail",
                CreatedAt = now
            });
            setupContext.Works.Add(new Work
            {
                Id = workId,
                Slug = slug,
                Title = "Postgres Public Work With Videos",
                Excerpt = "Postgres Public Work With Videos excerpt",
                Category = "case-study",
                ContentJson = contentJson,
                AllPropertiesJson = """{"adminOnly":"This must not be selected by public detail."}""",
                PublicContentHtml = "<p>Stored public video detail</p>",
                PublicContentMarkdown = "Stored public video detail",
                ThumbnailAssetId = thumbnailAssetId,
                PublicThumbnailUrl = expectedThumbnailUrl,
                PublicIconUrl = "/media/postgres-public-work-with-videos-icon.png",
                VideosVersion = 1,
                PublicVideosJson =
                    $$"""
                    [{
                      "id": "{{videoId}}",
                      "sourceType": "{{WorkVideoSourceTypes.YouTube}}",
                      "sourceKey": "dQw4w9WgXcQ",
                      "originalFileName": "Postgres public work detail video",
                      "sortOrder": 0,
                      "createdAt": "{{createdAt:O}}"
                    }]
                    """,
                Published = true,
                PublishedAt = now.AddMinutes(-1),
                CreatedAt = now,
                UpdatedAt = now
            });
            await setupContext.SaveChangesAsync(cancellationToken);
            await setupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE "Works"
                SET
                    "PublicContentHtml" = {"<p>Stored public video detail</p>"},
                    "PublicContentMarkdown" = {"Stored public video detail"},
                    "PublicThumbnailUrl" = {expectedThumbnailUrl}
                WHERE "Id" = {workId}
                """,
                cancellationToken);
        }

        var resolverThumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            thumbnailAssetId,
            contentJson,
            [
                new WorkVideo
                {
                    WorkId = workId,
                    SourceType = WorkVideoSourceTypes.YouTube,
                    SourceKey = "dQw4w9WgXcQ",
                    SortOrder = 0,
                    CreatedAt = now
                }
            ],
            new Dictionary<Guid, string> { [thumbnailAssetId] = expectedThumbnailUrl });
        var commandTextCapture = new CommandTextCaptureInterceptor();
        await using var dbContext = _fixture.CreateDbContext(commandTextCapture);
        var queryStore = new WorkQueryStore(dbContext, new NoopPlaybackUrlBuilder());

        var result = await queryStore.GetPublishedDetailBySlugAsync(slug, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(resolverThumbnailUrl, result!.ThumbnailUrl);
        Assert.Null(result.Content.Html);
        Assert.Equal("Stored public video detail", result.Content.Markdown);
        Assert.Equal(1, result.VideosVersion);
        var video = Assert.Single(result.Videos);
        Assert.Equal(videoId, video.Id);
        Assert.Equal(WorkVideoSourceTypes.YouTube, video.SourceType);
        Assert.Equal("dQw4w9WgXcQ", video.SourceKey);

        var commandText = Assert.Single(commandTextCapture.CommandTexts);
        Assert.DoesNotContain(
            "\"WorkVideos\"",
            commandText,
            StringComparison.OrdinalIgnoreCase);

        var combinedCommandText = string.Join('\n', commandTextCapture.CommandTexts);
        Assert.DoesNotContain("\"ContentJson\"", combinedCommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"AllPropertiesJson\"", combinedCommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Assets\"", combinedCommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"PublicIconUrl\"", combinedCommandText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicBlogDetail_UsesSinglePostgresCommand()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string slug = "postgres-public-blog-detail";
        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Blogs.Add(CreatePublishedBlog(slug, "Postgres Public Blog Detail", publishedAtOffsetMinutes: -1));
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var collector = CreateDiagnosticsCollector();
        await using var dbContext = _fixture.CreateDbContext(new LoadTestDbCommandDiagnosticsInterceptor(collector));
        var queryStore = new BlogQueryStore(dbContext);

        var result = await queryStore.GetPublishedDetailBySlugAsync(slug, cancellationToken);
        var snapshot = collector.CaptureSnapshot();

        Assert.NotNull(result);
        Assert.Equal($"/media/{slug}.png", result!.CoverUrl);
        Assert.Equal(1, snapshot.CommandLatency.SampleCount);
    }

    [Fact]
    public async Task PublicWorkFirstPage_UsesSinglePostgresCommand_ForNoSearchList()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string expectedThumbnailUrl = "/media/public-work-window-1-explicit-thumb.png";
        const string contentJson = """{"html":"<p><img src=\"/media/list-body-fallback-should-not-win.png\" alt=\"body\"></p>"}""";
        var workId = Guid.NewGuid();
        var thumbnailAssetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Assets.Add(new Asset
            {
                Id = thumbnailAssetId,
                Bucket = "media",
                Path = "public-work-window-1-explicit-thumb.png",
                PublicUrl = expectedThumbnailUrl,
                MimeType = "image/png",
                Kind = "work-thumbnail",
                CreatedAt = now
            });
            setupContext.Works.AddRange(
                new Work
                {
                    Id = workId,
                    Slug = "public-work-window-1",
                    Title = "Public Work Window 1",
                    Excerpt = "Public Work Window 1 excerpt",
                    Category = "case-study",
                    ContentJson = contentJson,
                    AllPropertiesJson = "{}",
                    PublicContentHtml = "<p>Public Work Window 1</p>",
                    ThumbnailAssetId = thumbnailAssetId,
                    PublicThumbnailUrl = expectedThumbnailUrl,
                    PublicIconUrl = "/media/public-work-window-1-icon.png",
                    Published = true,
                    PublishedAt = now.AddMinutes(-1),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                CreatePublishedWork("public-work-window-2", "Public Work Window 2", publishedAtOffsetMinutes: -2),
                CreatePublishedWork("public-work-window-3", "Public Work Window 3", publishedAtOffsetMinutes: -3));
            setupContext.WorkVideos.Add(new WorkVideo
            {
                WorkId = workId,
                SourceType = WorkVideoSourceTypes.YouTube,
                SourceKey = "list-video-fallback-should-not-win",
                OriginalFileName = "List video fallback should not win",
                SortOrder = 0,
                CreatedAt = now
            });
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var resolverThumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            thumbnailAssetId,
            contentJson,
            [
                new WorkVideo
                {
                    WorkId = workId,
                    SourceType = WorkVideoSourceTypes.YouTube,
                    SourceKey = "list-video-fallback-should-not-win",
                    SortOrder = 0,
                    CreatedAt = now
                }
            ],
            new Dictionary<Guid, string> { [thumbnailAssetId] = expectedThumbnailUrl });
        var commandTextCapture = new CommandTextCaptureInterceptor();
        await using var dbContext = _fixture.CreateDbContext(commandTextCapture);
        var queryStore = new WorkQueryStore(dbContext, new NoopPlaybackUrlBuilder());

        var result = await queryStore.GetPublishedPageAsync(1, 12, null, ContentSearchMode.Unified, cancellationToken);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(resolverThumbnailUrl, result.Items[0].ThumbnailUrl);
        var commandText = Assert.Single(commandTextCapture.CommandTexts);
        Assert.DoesNotContain("\"Period\"", commandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"PublicIconUrl\"", commandText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicBlogFirstPage_UsesSinglePostgresCommand_ForNoSearchList()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Blogs.AddRange(
                CreatePublishedBlog("public-blog-window-1", "Public Blog Window 1", publishedAtOffsetMinutes: -1),
                CreatePublishedBlog("public-blog-window-2", "Public Blog Window 2", publishedAtOffsetMinutes: -2),
                CreatePublishedBlog("public-blog-window-3", "Public Blog Window 3", publishedAtOffsetMinutes: -3));
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var collector = CreateDiagnosticsCollector();
        await using var dbContext = _fixture.CreateDbContext(new LoadTestDbCommandDiagnosticsInterceptor(collector));
        var queryStore = new BlogQueryStore(dbContext);

        var result = await queryStore.GetPublishedPageAsync(1, 12, null, ContentSearchMode.Unified, cancellationToken);
        var snapshot = collector.CaptureSnapshot();

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, snapshot.CommandLatency.SampleCount);
    }

    [Fact]
    public async Task PublicHome_UsesThreePostgresCommands_ForShellAndSummaryProjections()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        var resumeAssetId = Guid.NewGuid();

        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await setupContext.Database.EnsureCreatedAsync(cancellationToken);

            setupContext.Assets.Add(new Asset
            {
                Id = resumeAssetId,
                Bucket = "media",
                Path = "resume/home-shell.pdf",
                PublicUrl = "/media/resume/home-shell.pdf",
                MimeType = "application/pdf",
                Kind = "resume",
                CreatedAt = now
            });
            setupContext.Pages.Add(new PageEntity
            {
                Id = Guid.NewGuid(),
                Slug = "home",
                Title = "Home Shell",
                ContentJson = """{"headline":"Public home shell"}"""
            });
            setupContext.SiteSettings.Add(new SiteSetting
            {
                Singleton = true,
                OwnerName = "Home Owner",
                Tagline = "Home Tagline",
                GitHubUrl = "https://github.example/home",
                LinkedInUrl = "https://linkedin.example/home",
                ResumeAssetId = resumeAssetId
            });
            setupContext.Works.AddRange(
                CreatePublishedWork("public-home-work-1", "Public Home Work 1", publishedAtOffsetMinutes: -1),
                CreatePublishedWork("public-home-work-2", "Public Home Work 2", publishedAtOffsetMinutes: -2));
            setupContext.Blogs.AddRange(
                CreatePublishedBlog("public-home-blog-1", "Public Home Blog 1", publishedAtOffsetMinutes: -1),
                CreatePublishedBlog("public-home-blog-2", "Public Home Blog 2", publishedAtOffsetMinutes: -2));
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var collector = CreateDiagnosticsCollector();
        await using var dbContext = _fixture.CreateDbContext(new LoadTestDbCommandDiagnosticsInterceptor(collector));
        var queryStore = new HomeQueryStore(dbContext);
        var handler = new GetHomeQueryHandler(queryStore);

        var result = await handler.Handle(new GetHomeQuery(), cancellationToken);
        var snapshot = collector.CaptureSnapshot();

        Assert.NotNull(result);
        Assert.Equal("Home Shell", result!.HomePage.Title);
        Assert.Equal("/media/resume/home-shell.pdf", result.SiteSettings.ResumePublicUrl);
        Assert.Equal(2, result.FeaturedWorks.Count);
        Assert.Equal(2, result.RecentPosts.Count);
        Assert.Equal(3, snapshot.CommandLatency.SampleCount);
    }

    [Fact]
    public async Task AdminWorkList_UsesSinglePostgresCommand_AndStoredThumbnail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string slug = "admin-work-list-projection";
        const string expectedThumbnailUrl = "/media/admin-work-list-thumb.png";
        var workId = Guid.NewGuid();
        var thumbnailAssetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        const string contentJson = """{"html":"<p>This body must not be needed by the admin list DTO.</p>"}""";

        await using (var setupContext = CreateDbContext())
        {
            await ResetDatabaseAsync(setupContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(setupContext, cancellationToken);
            await ClearPublicContentAsync(setupContext, cancellationToken);

            setupContext.Assets.Add(new Asset
            {
                Id = thumbnailAssetId,
                Bucket = "media",
                Path = "admin-work-list-asset-thumb.png",
                PublicUrl = expectedThumbnailUrl,
                MimeType = "image/png",
                Kind = "work-thumbnail",
                CreatedAt = now
            });
            setupContext.Works.Add(new Work
            {
                Id = workId,
                Slug = slug,
                Title = "Admin Work List Projection",
                Excerpt = "List projection should use stored public fields",
                Category = "case-study",
                ContentJson = contentJson,
                AllPropertiesJson = """{"unused":"This metadata must not be needed by the admin list DTO."}""",
                ThumbnailAssetId = thumbnailAssetId,
                PublicThumbnailUrl = expectedThumbnailUrl,
                Published = true,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
            setupContext.WorkVideos.Add(new WorkVideo
            {
                WorkId = workId,
                SourceType = WorkVideoSourceTypes.YouTube,
                SourceKey = "dQw4w9WgXcQ",
                OriginalFileName = "Admin list should not need this video",
                SortOrder = 0,
                CreatedAt = now
            });
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var resolverThumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            thumbnailAssetId,
            contentJson,
            [
                new WorkVideo
                {
                    WorkId = workId,
                    SourceType = WorkVideoSourceTypes.YouTube,
                    SourceKey = "dQw4w9WgXcQ",
                    SortOrder = 0,
                    CreatedAt = now
                }
            ],
            new Dictionary<Guid, string> { [thumbnailAssetId] = expectedThumbnailUrl });
        var collector = CreateDiagnosticsCollector();
        await using var dbContext = _fixture.CreateDbContext(new LoadTestDbCommandDiagnosticsInterceptor(collector));
        var queryStore = new WorkQueryStore(dbContext, new NoopPlaybackUrlBuilder());

        var result = await queryStore.GetAdminListAsync(cancellationToken);
        var snapshot = collector.CaptureSnapshot();

        var item = Assert.Single(result, work => work.Slug == slug);
        Assert.Equal(resolverThumbnailUrl, item.ThumbnailUrl);
        Assert.Equal(1, snapshot.CommandLatency.SampleCount);
    }

    private WoongBlogDbContext CreateDbContext()
    {
        return _fixture.CreateDbContext();
    }

    private static async Task ResetDatabaseAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private static async Task<DataCounts> CountSeededDataAsync(
        WoongBlogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return new DataCounts(
            await dbContext.Blogs.CountAsync(cancellationToken),
            await dbContext.Works.CountAsync(cancellationToken),
            await dbContext.Pages.CountAsync(cancellationToken),
            await dbContext.SiteSettings.CountAsync(cancellationToken),
            await dbContext.SchemaPatches.CountAsync(cancellationToken));
    }

    private static async Task ClearPublicContentAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.WorkVideos.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Works.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Blogs.ExecuteDeleteAsync(cancellationToken);
    }

    private static Blog CreateBlog(string slug, string title)
    {
        return new Blog
        {
            Slug = slug,
            Title = title,
            Excerpt = $"{title} excerpt",
            ContentJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow
        };
    }

    private static Blog CreatePublishedBlog(string slug, string title, int publishedAtOffsetMinutes)
    {
        var now = DateTimeOffset.UtcNow;
        return new Blog
        {
            Slug = slug,
            Title = title,
            Excerpt = $"{title} excerpt",
            ContentJson = "{}",
            PublicContentHtml = $"<p>{title}</p>",
            PublicCoverUrl = $"/media/{slug}.png",
            Published = true,
            PublishedAt = now.AddMinutes(publishedAtOffsetMinutes),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static DatabaseDiagnosticsCollector CreateDiagnosticsCollector()
    {
        return new DatabaseDiagnosticsCollector(Options.Create(new DatabaseDiagnosticsOptions
        {
            LatencySampleCapacity = 32,
            SlowQuerySampleCapacity = 8,
            SlowQueryThresholdMs = 1_000_000
        }));
    }

    private static Work CreateWork(string slug)
    {
        return new Work
        {
            Slug = slug,
            Title = "Cascade Work",
            Excerpt = "Cascade work excerpt",
            Category = "case-study",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow
        };
    }

    private static Work CreatePublishedWork(string slug, string title, int publishedAtOffsetMinutes)
    {
        var now = DateTimeOffset.UtcNow;
        return new Work
        {
            Slug = slug,
            Title = title,
            Excerpt = $"{title} excerpt",
            Category = "case-study",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            PublicContentHtml = $"<p>{title}</p>",
            PublicThumbnailUrl = $"/media/{slug}-thumb.png",
            PublicIconUrl = $"/media/{slug}-icon.png",
            Published = true,
            PublishedAt = now.AddMinutes(publishedAtOffsetMinutes),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static async Task AssertPostgresConstraintViolationAsync(
        string expectedSqlState,
        Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(action);
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(expectedSqlState, postgresException.SqlState);
    }

    private static async Task<bool> ExistsAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static async Task<string> ScalarStringAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Assert.IsType<string>(result);
    }

    private sealed class CommandTextCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> CommandTexts { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CommandTexts.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CommandTexts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed record DataCounts(
        int Blogs,
        int Works,
        int Pages,
        int SiteSettings,
        int SchemaPatches);

    private sealed class NoopPlaybackUrlBuilder : IWorkVideoPlaybackUrlBuilder
    {
        public string? BuildPlaybackUrl(string sourceType, string sourceKey) => null;

        public string? BuildStorageObjectUrl(string storageType, string storageKey) => null;

        public Task<bool> StorageObjectExistsAsync(
            string storageType,
            string storageKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("portfolio")
            .WithUsername("portfolio")
            .WithPassword("portfolio")
            .Build();

        public async ValueTask InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        public WoongBlogDbContext CreateDbContext(DbCommandInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<WoongBlogDbContext>()
                .UseNpgsql(_postgres.GetConnectionString());

            if (interceptor is not null)
            {
                builder.AddInterceptors(interceptor);
            }

            return new WoongBlogDbContext(builder.Options);
        }
    }
}
