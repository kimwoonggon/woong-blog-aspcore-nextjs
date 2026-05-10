using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.Abstractions;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Infrastructure.Modules.Composition.Persistence;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Infrastructure.Modules.Content.Blogs.Persistence;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;
using WoongBlog.Infrastructure.Modules.Content.Pages.Persistence;
using WoongBlog.Application.Modules.Content.Works.Abstractions;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorkById;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Modules.Content.Works.Persistence;
using WoongBlog.Application.Modules.Site.Abstractions;
using WoongBlog.Application.Modules.Site.GetResume;
using WoongBlog.Application.Modules.Site.GetSiteSettings;
using WoongBlog.Infrastructure.Modules.Site.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public class PublicQueryHandlerComponentTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static IHomeQueryStore CreateHomeQueryStore(WoongBlogDbContext dbContext) => new HomeQueryStore(dbContext);
    private static ISiteSettingsQueryStore CreateSiteSettingsQueryStore(WoongBlogDbContext dbContext) => new SiteSettingsQueryStore(dbContext);
    private static IWorkQueryStore CreateWorkQueryStore(WoongBlogDbContext dbContext, TestPlaybackUrlBuilder? playbackUrlBuilder = null)
        => new WorkQueryStore(dbContext, playbackUrlBuilder ?? new TestPlaybackUrlBuilder());
    private static IBlogQueryStore CreateBlogQueryStore(WoongBlogDbContext dbContext) => new BlogQueryStore(dbContext);
    private static IPageQueryStore CreatePageQueryStore(WoongBlogDbContext dbContext) => new PageQueryStore(dbContext);

    private sealed class TestPlaybackUrlBuilder : IWorkVideoPlaybackUrlBuilder
    {
        public HashSet<string> ExistingStorageKeys { get; } = [];
        public int StorageObjectExistsCallCount { get; private set; }

        public string? BuildPlaybackUrl(string sourceType, string sourceKey)
        {
            return sourceType == WorkVideoSourceTypes.YouTube ? null : $"/media/{sourceKey}";
        }

        public string? BuildStorageObjectUrl(string storageType, string storageKey)
        {
            return $"/media/{storageKey}";
        }

        public Task<bool> StorageObjectExistsAsync(string storageType, string storageKey, CancellationToken cancellationToken)
        {
            StorageObjectExistsCallCount += 1;
            return Task.FromResult(ExistingStorageKeys.Contains(storageKey));
        }
    }

    [Fact]
    public async Task GetHomeQueryHandler_ReturnsNull_WhenHomePageMissing()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Owner",
            Tagline = "Tagline"
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHomeQueryHandler_ReturnsOnlyPublishedContent()
    {
        await using var dbContext = CreateDbContext();
        var resumeAssetId = Guid.NewGuid();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Owner",
            Tagline = "Tagline",
            ResumeAssetId = resumeAssetId
        });
        dbContext.Assets.Add(new Asset { Id = resumeAssetId, Bucket = "media", Path = "resume.pdf", PublicUrl = "/media/resume.pdf" });
        dbContext.Pages.Add(new PageEntity { Id = Guid.NewGuid(), Slug = "home", Title = "Home", ContentJson = "{\"headline\":\"Hi\"}" });
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Published Work", Slug = "published-work", Excerpt = "published", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Draft Work", Slug = "draft-work", Excerpt = "draft", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", Published = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        dbContext.Blogs.AddRange(
            new Blog { Id = Guid.NewGuid(), Title = "Published Blog", Slug = "published-blog", Excerpt = "published", ContentJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Blog { Id = Guid.NewGuid(), Title = "Draft Blog", Slug = "draft-blog", Excerpt = "draft", ContentJson = "{}", Published = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.FeaturedWorks);
        Assert.Single(result.RecentPosts);
        Assert.Equal("/media/resume.pdf", result.SiteSettings.ResumePublicUrl);
    }

    [Fact]
    public async Task GetResumeQueryHandler_ReturnsNull_WhenResumeAssetMissing()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Owner",
            Tagline = "Tagline",
            ResumeAssetId = Guid.NewGuid()
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetResumeQueryHandler(CreateSiteSettingsQueryStore(dbContext));

        var result = await handler.Handle(new GetResumeQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSiteSettingsQueryHandler_ReturnsNull_WhenSettingsMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetSiteSettingsQueryHandler(CreateSiteSettingsQueryStore(dbContext));

        var result = await handler.Handle(new GetSiteSettingsQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHomeQueryHandler_ReturnsNull_WhenSiteSettingsMissing()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Pages.Add(new PageEntity
        {
            Id = Guid.NewGuid(),
            Slug = "home",
            Title = "Home",
            ContentJson = """{"headline":"Hello"}"""
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHomeQueryHandler_MapsHomeContentAndOrdersPublishedSummaries()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        var resumeAssetId = Guid.NewGuid();
        var thumbnailAssetId = Guid.NewGuid();
        var iconAssetId = Guid.NewGuid();
        var coverAssetId = Guid.NewGuid();
        dbContext.Assets.AddRange(
            new Asset { Id = resumeAssetId, Bucket = "media", Path = "resume.pdf", PublicUrl = "/media/resume.pdf" },
            new Asset { Id = thumbnailAssetId, Bucket = "media", Path = "work-thumb.png", PublicUrl = "/media/work-thumb.png" },
            new Asset { Id = iconAssetId, Bucket = "media", Path = "work-icon.png", PublicUrl = "/media/work-icon.png" },
            new Asset { Id = coverAssetId, Bucket = "media", Path = "blog-cover.png", PublicUrl = "/media/blog-cover.png" });
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Public Owner",
            Tagline = "Public Tagline",
            GitHubUrl = "https://github.example/public",
            LinkedInUrl = "https://linkedin.example/public",
            ResumeAssetId = resumeAssetId
        });
        dbContext.Pages.Add(new PageEntity
        {
            Id = Guid.NewGuid(),
            Slug = "home",
            Title = "Home",
            ContentJson = """{"headline":"Public Headline","introText":"Public intro"}"""
        });
        dbContext.Works.AddRange(
            CreateWork(
                "Older Work",
                "older-work",
                published: true,
                publishedAt: now.AddDays(-3),
                thumbnailAssetId,
                iconAssetId,
                publicThumbnailUrl: "/media/work-thumb.png",
                publicIconUrl: "/media/work-icon.png"),
            CreateWork("Newer Work", "newer-work", published: true, publishedAt: now.AddDays(-1)),
            CreateWork("Draft Work", "draft-work", published: false, publishedAt: now));
        dbContext.Blogs.AddRange(
            CreateBlog(
                "Older Blog",
                "older-blog",
                published: true,
                publishedAt: now.AddDays(-4),
                coverAssetId,
                publicCoverUrl: "/media/blog-cover.png"),
            CreateBlog("Newer Blog", "newer-blog", published: true, publishedAt: now.AddDays(-2)),
            CreateBlog("Draft Blog", "draft-blog", published: false, publishedAt: now));
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Home", result!.HomePage.Title);
        Assert.Contains("Public Headline", result.HomePage.ContentJson, StringComparison.Ordinal);
        Assert.Equal("Public Owner", result.SiteSettings.OwnerName);
        Assert.Equal("Public Tagline", result.SiteSettings.Tagline);
        Assert.Equal("https://github.example/public", result.SiteSettings.GitHubUrl);
        Assert.Equal("https://linkedin.example/public", result.SiteSettings.LinkedInUrl);
        Assert.Equal("/media/resume.pdf", result.SiteSettings.ResumePublicUrl);
        Assert.Equal(new[] { "Newer Work", "Older Work" }, result.FeaturedWorks.Select(x => x.Title).ToArray());
        Assert.Equal(new[] { "Newer Blog", "Older Blog" }, result.RecentPosts.Select(x => x.Title).ToArray());
        Assert.Equal("/media/work-thumb.png", result.FeaturedWorks[1].ThumbnailUrl);
        Assert.Equal("/media/blog-cover.png", result.RecentPosts[1].CoverUrl);
    }

    [Fact]
    public async Task GetHomeQueryHandler_UsesStoredPublicMediaUrlsWithoutAssetRows()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Public Owner",
            Tagline = "Public Tagline"
        });
        dbContext.Pages.Add(new PageEntity
        {
            Id = Guid.NewGuid(),
            Slug = "home",
            Title = "Home",
            ContentJson = "{}"
        });
        dbContext.Works.Add(new Work
        {
            Id = Guid.NewGuid(),
            Title = "Stored Media Work",
            Slug = "stored-media-work",
            Excerpt = "stored media",
            Category = "public",
            Period = "2026",
            Tags = ["public"],
            ThumbnailAssetId = Guid.NewGuid(),
            IconAssetId = Guid.NewGuid(),
            PublicThumbnailUrl = "/media/stored-home-work-thumb.png",
            PublicIconUrl = "/media/stored-home-work-icon.png",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.Blogs.Add(new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Stored Media Blog",
            Slug = "stored-media-blog",
            Excerpt = "stored media",
            Tags = ["public"],
            CoverAssetId = Guid.NewGuid(),
            PublicCoverUrl = "/media/stored-home-blog-cover.png",
            ContentJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.NotNull(result);
        var work = Assert.Single(result!.FeaturedWorks);
        var blog = Assert.Single(result.RecentPosts);
        Assert.Equal("/media/stored-home-work-thumb.png", work.ThumbnailUrl);
        Assert.Equal("/media/stored-home-blog-cover.png", blog.CoverUrl);
    }

    [Fact]
    public async Task GetHomeQueryHandler_DoesNotUseWorkBodyImageAsFeaturedThumbnailFallback()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Public Owner",
            Tagline = "Public Tagline"
        });
        dbContext.Pages.Add(new PageEntity
        {
            Id = Guid.NewGuid(),
            Slug = "home",
            Title = "Home",
            ContentJson = "{}"
        });
        dbContext.Works.Add(new Work
        {
            Id = Guid.NewGuid(),
            Title = "Inline Body Image Work",
            Slug = "inline-body-image-work",
            Excerpt = "published",
            Category = "public",
            Period = "2026",
            Tags = ["public", "test"],
            ContentJson = """{"html":"<p><img src=\"/media/body-inline-only.png\" alt=\"body\"></p>"}""",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.NotNull(result);
        var featuredWork = Assert.Single(result!.FeaturedWorks);
        Assert.Equal(string.Empty, featuredWork.ThumbnailUrl);
    }

    [Fact]
    public async Task GetHomeQueryHandler_DoesNotUseWorkVideoAsFeaturedThumbnailFallback()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);
        var workId = Guid.NewGuid();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Public Owner",
            Tagline = "Public Tagline"
        });
        dbContext.Pages.Add(new PageEntity
        {
            Id = Guid.NewGuid(),
            Slug = "home",
            Title = "Home",
            ContentJson = "{}"
        });
        dbContext.Works.Add(new Work
        {
            Id = workId,
            Title = "Video Thumbnail Work",
            Slug = "video-thumbnail-work",
            Excerpt = "published",
            Category = "public",
            Period = "2026",
            Tags = ["public", "test"],
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.WorkVideos.Add(new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = "home-video-fallback",
            SortOrder = 0,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.NotNull(result);
        var featuredWork = Assert.Single(result!.FeaturedWorks);
        Assert.Equal(string.Empty, featuredWork.ThumbnailUrl);
    }

    [Fact]
    public async Task GetSiteSettingsQueryHandler_ReturnsAllPublicSocialFields()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Owner",
            Tagline = "Tagline",
            FacebookUrl = "https://facebook.example/owner",
            InstagramUrl = "https://instagram.example/owner",
            TwitterUrl = "https://twitter.example/owner",
            LinkedInUrl = "https://linkedin.example/owner",
            GitHubUrl = "https://github.example/owner",
            ResumeAssetId = Guid.NewGuid()
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetSiteSettingsQueryHandler(CreateSiteSettingsQueryStore(dbContext));

        var result = await handler.Handle(new GetSiteSettingsQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Owner", result!.OwnerName);
        Assert.Equal("Tagline", result.Tagline);
        Assert.Equal("https://facebook.example/owner", result.FacebookUrl);
        Assert.Equal("https://instagram.example/owner", result.InstagramUrl);
        Assert.Equal("https://twitter.example/owner", result.TwitterUrl);
        Assert.Equal("https://linkedin.example/owner", result.LinkedInUrl);
        Assert.Equal("https://github.example/owner", result.GitHubUrl);
    }

    [Fact]
    public async Task GetPageBySlugQueryHandler_ReturnsPageContent_WhenSlugExists()
    {
        await using var dbContext = CreateDbContext();
        var pageId = Guid.NewGuid();
        dbContext.Pages.Add(new PageEntity
        {
            Id = pageId,
            Slug = "introduction",
            Title = "Introduction",
            ContentJson = """{"html":"<p>Public introduction</p>"}"""
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetPageBySlugQueryHandler(CreatePageQueryStore(dbContext));

        var result = await handler.Handle(new GetPageBySlugQuery("introduction"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(pageId, result!.Id);
        Assert.Equal("introduction", result.Slug);
        Assert.Equal("Introduction", result.Title);
        Assert.Contains("Public introduction", result.ContentJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPageBySlugQueryHandler_ReturnsNull_WhenSlugIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetPageBySlugQueryHandler(CreatePageQueryStore(dbContext));

        var result = await handler.Handle(new GetPageBySlugQuery("missing-page"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorksQueryHandler_SkipsDrafts_AndUsesStoredPublicMediaUrlsWithoutAssetRows()
    {
        await using var dbContext = CreateDbContext();
        var thumbnailId = Guid.NewGuid();
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Visible", Slug = "visible", Excerpt = "visible", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", ThumbnailAssetId = thumbnailId, PublicThumbnailUrl = "/media/thumb.png", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Hidden", Slug = "hidden", Excerpt = "hidden", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", Published = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("/media/thumb.png", result.Items[0].ThumbnailUrl);
    }

    [Fact]
    public async Task GetWorksQueryHandler_DoesNotUseWorkVideoAsPublicThumbnailFallback()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);
        var workId = Guid.NewGuid();
        dbContext.Works.Add(new Work
        {
            Id = workId,
            Title = "Video Card Work",
            Slug = "video-card-work",
            Excerpt = "published",
            Category = "public",
            Period = "2026",
            Tags = ["public", "test"],
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.WorkVideos.Add(new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = "list-video-fallback",
            SortOrder = 0,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Page: 1, PageSize: 12), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("video-card-work", item.Slug);
        Assert.Equal(string.Empty, item.ThumbnailUrl);
    }

    [Fact]
    public async Task GetWorksQueryHandler_ReturnsPagedResults()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Work A", Slug = "work-a", Excerpt = "a", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow.AddDays(-1), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Work B", Slug = "work-b", Excerpt = "b", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Page: 2, PageSize: 1), CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetWorksQueryHandler_ReturnsStableEmptyPage_WhenNoWorksArePublished()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.Add(CreateWork("Draft Work", "draft-work", published: false, publishedAt: null));
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Page: 4, PageSize: 2), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetWorksQueryHandler_FiltersDraftsOrdersByPublishedAtAndMapsStoredPublicMediaUrls()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        var thumbnailAssetId = Guid.NewGuid();
        var iconAssetId = Guid.NewGuid();
        var draftThumbnailAssetId = Guid.NewGuid();
        dbContext.Assets.AddRange(
            new Asset { Id = thumbnailAssetId, Bucket = "media", Path = "published-thumb.png", PublicUrl = "/media/published-thumb.png" },
            new Asset { Id = iconAssetId, Bucket = "media", Path = "published-icon.png", PublicUrl = "/media/published-icon.png" },
            new Asset { Id = draftThumbnailAssetId, Bucket = "media", Path = "draft-thumb.png", PublicUrl = "/media/draft-thumb.png" });
        dbContext.Works.AddRange(
            CreateWork(
                "Older Work",
                "older-work",
                published: true,
                publishedAt: now.AddDays(-3),
                thumbnailAssetId,
                iconAssetId,
                publicThumbnailUrl: "/media/published-thumb.png",
                publicIconUrl: "/media/published-icon.png"),
            CreateWork("Newer Work", "newer-work", published: true, publishedAt: now.AddDays(-1)),
            CreateWork("Draft Work", "draft-work", published: false, publishedAt: now, draftThumbnailAssetId));
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Page: 1, PageSize: 10), CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(new[] { "Newer Work", "Older Work" }, result.Items.Select(x => x.Title).ToArray());
        Assert.DoesNotContain(result.Items, x => x.Slug == "draft-work");
        Assert.Equal("/media/published-thumb.png", result.Items[1].ThumbnailUrl);
    }

    [Fact]
    public async Task GetWorksQueryHandler_ClampsRequestedPageBeyondLastPage()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        dbContext.Works.AddRange(
            CreateWork("Older Work", "older-work", published: true, publishedAt: now.AddDays(-2)),
            CreateWork("Newer Work", "newer-work", published: true, publishedAt: now.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Page: 99, PageSize: 1), CancellationToken.None);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("Older Work", result.Items[0].Title);
    }

    [Fact]
    public async Task GetWorksQueryHandler_FiltersByTitleSearch()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Searchable Terminal UI", Slug = "searchable-terminal-ui", Excerpt = "alpha", Category = "cat", ContentJson = "{\"html\":\"<p>alpha</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Different Work", Slug = "different-work", Excerpt = "beta", Category = "cat", ContentJson = "{\"html\":\"<p>beta</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Query: "terminal", SearchMode: "title"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Searchable Terminal UI", result.Items[0].Title);
    }

    [Fact]
    public async Task GetWorksQueryHandler_FiltersByNormalizedTitleSearch()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "T,B,N 안녕하세요", Slug = "tbn-work", Excerpt = "alpha", Category = "cat", ContentJson = "{\"html\":\"<p>alpha</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Different Work", Slug = "different-work", Excerpt = "beta", Category = "cat", ContentJson = "{\"html\":\"<p>beta</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Query: "tbn", SearchMode: "title"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("T,B,N 안녕하세요", result.Items[0].Title);
    }

    [Fact]
    public async Task GetWorksQueryHandler_FiltersByContentSearch()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Content Match", Slug = "content-match", Excerpt = "contains graph-token", Category = "cat", ContentJson = "{\"html\":\"<p>alpha</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Miss", Slug = "miss", Excerpt = "beta", Category = "cat", ContentJson = "{\"html\":\"<p>beta</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Query: "graph-token", SearchMode: "content"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Content Match", result.Items[0].Title);
    }

    [Fact]
    public async Task GetWorksQueryHandler_QueryOnly_PerformsUnifiedSearch()
    {
        await using var dbContext = CreateDbContext();
        var bodyToken = $"body-token-{Guid.NewGuid():N}";
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Unified Title Match", Slug = "unified-title-match", Excerpt = "alpha", Category = "cat", ContentJson = "{\"html\":\"<p>alpha</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "No title token", Slug = "unified-content-match", Excerpt = $"contains {bodyToken}", Category = "cat", ContentJson = "{\"html\":\"<p>beta</p>\"}", AllPropertiesJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var titleResult = await handler.Handle(new GetWorksQuery(Query: "unified title"), CancellationToken.None);
        var contentResult = await handler.Handle(new GetWorksQuery(Query: bodyToken), CancellationToken.None);

        Assert.Single(titleResult.Items);
        Assert.Equal("Unified Title Match", titleResult.Items[0].Title);
        Assert.Single(contentResult.Items);
        Assert.Equal("No title token", contentResult.Items[0].Title);
    }

    [Fact]
    public async Task GetHomeQueryHandler_ReturnsUpToSixRecentPosts()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Owner",
            Tagline = "Tagline",
        });
        dbContext.Pages.Add(new PageEntity { Id = Guid.NewGuid(), Slug = "home", Title = "Home", ContentJson = "{}" });

        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < 8; index += 1)
        {
            dbContext.Blogs.Add(new Blog
            {
                Id = Guid.NewGuid(),
                Title = $"Published Blog {index}",
                Slug = $"published-blog-{index}",
                Excerpt = "published",
                ContentJson = "{}",
                Published = true,
                PublishedAt = now.AddMinutes(-index),
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();
        var handler = new GetHomeQueryHandler(CreateHomeQueryStore(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(6, result!.RecentPosts.Count);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_MapsSocialShareMessage_FromStoredPublicReadModel()
    {
        await using var dbContext = CreateDbContext();
        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = "Share-ready Work",
            Slug = "share-ready-work",
            Excerpt = "default excerpt",
            Category = "cat",
            ContentJson = "{}",
            AllPropertiesJson = """{"socialShareMessage":"Stored public share message"}""",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Works.Add(work);
        await dbContext.SaveChangesAsync();

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorkBySlugQuery("share-ready-work"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Stored public share message", result!.SocialShareMessage);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_ReturnsNullSocialShareMessage_WhenReservedKeyMissing()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.Add(new Work
        {
            Id = Guid.NewGuid(),
            Title = "Share default Work",
            Slug = "share-default-work",
            Excerpt = "default excerpt",
            Category = "cat",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorkBySlugQuery("share-default-work"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.SocialShareMessage);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_MapsTimelinePreviewUrls_ForHlsVideo()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var previewVttStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewVttFileName}";
        var previewSpriteStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewSpriteFileName}";
        var now = DateTimeOffset.UtcNow;
        var storedVideo = new WorkVideo
        {
            Id = videoId,
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.Hls,
            SourceKey = $"{WorkVideoSourceTypes.Local}:videos/{workId:N}/{videoId:N}/hls/master.m3u8",
            MimeType = WorkVideoPolicy.HlsManifestContentType,
            TimelinePreviewVttStorageKey = previewVttStorageKey,
            TimelinePreviewSpriteStorageKey = previewSpriteStorageKey,
            SortOrder = 0,
            CreatedAt = now
        };
        dbContext.Works.Add(new Work
        {
            Id = workId,
            Title = "Preview Work",
            Slug = "preview-work",
            Excerpt = "preview",
            Category = "cat",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            VideosVersion = 1,
            PublicVideosJson = WorkPublicVideosReadModel.Serialize([storedVideo]),
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.WorkVideos.Add(storedVideo);
        await dbContext.SaveChangesAsync();

        var playbackUrlBuilder = new TestPlaybackUrlBuilder();
        playbackUrlBuilder.ExistingStorageKeys.Add(previewVttStorageKey);
        playbackUrlBuilder.ExistingStorageKeys.Add(previewSpriteStorageKey);

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext, playbackUrlBuilder));
        var result = await handler.Handle(new GetWorkBySlugQuery("preview-work"), CancellationToken.None);

        Assert.NotNull(result);
        var video = Assert.Single(result!.Videos);
        Assert.Equal($"/media/videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewVttFileName}", video.TimelinePreviewVttUrl);
        Assert.Equal($"/media/videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewSpriteFileName}", video.TimelinePreviewSpriteUrl);
        Assert.Equal(0, playbackUrlBuilder.StorageObjectExistsCallCount);
    }

    [Fact]
    public async Task GetAdminWorkByIdQueryHandler_VerifiesTimelinePreviewAssets()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var previewVttStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewVttFileName}";
        var previewSpriteStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewSpriteFileName}";
        var now = DateTimeOffset.UtcNow;
        dbContext.Works.Add(new Work
        {
            Id = workId,
            Title = "Admin Preview Work",
            Slug = "admin-preview-work",
            Excerpt = "preview",
            Category = "cat",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.WorkVideos.Add(new WorkVideo
        {
            Id = videoId,
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.Hls,
            SourceKey = $"{WorkVideoSourceTypes.Local}:videos/{workId:N}/{videoId:N}/hls/master.m3u8",
            MimeType = WorkVideoPolicy.HlsManifestContentType,
            TimelinePreviewVttStorageKey = previewVttStorageKey,
            TimelinePreviewSpriteStorageKey = previewSpriteStorageKey,
            SortOrder = 0,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var playbackUrlBuilder = new TestPlaybackUrlBuilder();
        playbackUrlBuilder.ExistingStorageKeys.Add(previewVttStorageKey);
        playbackUrlBuilder.ExistingStorageKeys.Add(previewSpriteStorageKey);
        var handler = new GetAdminWorkByIdQueryHandler(CreateWorkQueryStore(dbContext, playbackUrlBuilder));

        var result = await handler.Handle(new(workId), CancellationToken.None);

        Assert.NotNull(result);
        var video = Assert.Single(result!.Videos);
        Assert.NotNull(video.TimelinePreviewVttUrl);
        Assert.NotNull(video.TimelinePreviewSpriteUrl);
        Assert.Equal(2, playbackUrlBuilder.StorageObjectExistsCallCount);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_ReturnsPublishedDetailWithStoredPublicMediaUrlsAndVideos()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var thumbnailAssetId = Guid.NewGuid();
        var iconAssetId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        var secondVideo = new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.Local,
            SourceKey = "videos/detail-second.mp4",
            OriginalFileName = "Second",
            MimeType = "video/mp4",
            FileSize = 200,
            SortOrder = 1,
            CreatedAt = now
        };
        var firstVideo = new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.Local,
            SourceKey = "videos/detail-first.mp4",
            OriginalFileName = "First",
            MimeType = "video/mp4",
            FileSize = 100,
            SortOrder = 0,
            CreatedAt = now.AddSeconds(-1)
        };
        var work = new Work
        {
            Id = workId,
            Title = "Public Work Detail",
            Slug = "public-work-detail",
            Excerpt = "detail excerpt",
            Category = "detail",
            Period = "2026",
            Tags = ["detail", "public"],
            ContentJson = """{"html":"<p>Public work detail body</p>"}""",
            AllPropertiesJson = """{"socialShareMessage":"Stored share detail"}""",
            ThumbnailAssetId = thumbnailAssetId,
            IconAssetId = iconAssetId,
            PublicThumbnailUrl = "/media/detail-thumb.png",
            PublicIconUrl = "/media/detail-icon.png",
            VideosVersion = 2,
            PublicVideosJson = WorkPublicVideosReadModel.Serialize([secondVideo, firstVideo]),
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Works.Add(work);
        dbContext.WorkVideos.AddRange(secondVideo, firstVideo);
        await dbContext.SaveChangesAsync();

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorkBySlugQuery("public-work-detail"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(workId, result!.Id);
        Assert.Equal("Public Work Detail", result.Title);
        Assert.Equal("detail", result.Category);
        Assert.Equal("2026", result.Period);
        Assert.Equal(new[] { "detail", "public" }, result.Tags);
        Assert.Contains("Public work detail body", result.Content.Html, StringComparison.Ordinal);
        Assert.Equal("/media/detail-thumb.png", result.ThumbnailUrl);
        Assert.Equal(now, result.PublishedAt);
        Assert.Equal("Stored share detail", result.SocialShareMessage);
        Assert.Equal(2, result.VideosVersion);
        Assert.Equal(new[] { firstVideo.Id, secondVideo.Id }, result.Videos.Select(x => x.Id).ToArray());
        Assert.Equal("/media/videos/detail-first.mp4", result.Videos[0].PlaybackUrl);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_ReturnsVideosButDoesNotUseVideoAsPublicThumbnailFallback()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);
        var storedVideo = new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = "detail-video-fallback",
            OriginalFileName = "Detail YouTube",
            SortOrder = 0,
            CreatedAt = now
        };
        dbContext.Works.Add(new Work
        {
            Id = workId,
            Title = "Video Detail Work",
            Slug = "video-detail-work",
            Excerpt = "detail excerpt",
            Category = "detail",
            Tags = ["detail", "public"],
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            VideosVersion = 1,
            PublicVideosJson = WorkPublicVideosReadModel.Serialize([storedVideo]),
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.WorkVideos.Add(storedVideo);
        await dbContext.SaveChangesAsync();

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorkBySlugQuery("video-detail-work"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.ThumbnailUrl);
        var video = Assert.Single(result.Videos);
        Assert.Equal(WorkVideoSourceTypes.YouTube, video.SourceType);
        Assert.Equal("detail-video-fallback", video.SourceKey);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_DoesNotUseBodyImageAsPublicThumbnailFallback()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        dbContext.Works.Add(new Work
        {
            Id = Guid.NewGuid(),
            Title = "Body Image Detail",
            Slug = "body-image-detail",
            Excerpt = "detail excerpt",
            Category = "detail",
            Tags = ["detail", "public"],
            ContentJson = """{"html":"<p><img src=\"/media/detail-body-inline.png\" alt=\"body\"></p>"}""",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorkBySlugQuery("body-image-detail"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("detail-body-inline.png", result!.Content.Html, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.ThumbnailUrl);
    }

    [Fact]
    public async Task GetWorkBySlugQueryHandler_ReturnsNull_ForDraftOrMissingSlug()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.Add(CreateWork("Draft Work", "draft-work", published: false, publishedAt: null));
        await dbContext.SaveChangesAsync();

        var handler = new GetWorkBySlugQueryHandler(CreateWorkQueryStore(dbContext));

        var draftResult = await handler.Handle(new GetWorkBySlugQuery("draft-work"), CancellationToken.None);
        var missingResult = await handler.Handle(new GetWorkBySlugQuery("missing-work"), CancellationToken.None);

        Assert.Null(draftResult);
        Assert.Null(missingResult);
    }

    [Fact]
    public async Task GetAdminWorkByIdQueryHandler_OmitsTimelinePreviewUrls_WhenPreviewAssetsAreMissing()
    {
        await using var dbContext = CreateDbContext();
        var workId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var previewVttStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewVttFileName}";
        var previewSpriteStorageKey = $"videos/{workId:N}/{videoId:N}/hls/{WorkVideoPolicy.TimelinePreviewSpriteFileName}";

        dbContext.Works.Add(new Work
        {
            Id = workId,
            Title = "Preview Missing Work",
            Slug = "preview-missing-work",
            Excerpt = "preview",
            Category = "cat",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        dbContext.WorkVideos.Add(new WorkVideo
        {
            Id = videoId,
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.Hls,
            SourceKey = $"{WorkVideoSourceTypes.Local}:videos/{workId:N}/{videoId:N}/hls/master.m3u8",
            MimeType = WorkVideoPolicy.HlsManifestContentType,
            TimelinePreviewVttStorageKey = previewVttStorageKey,
            TimelinePreviewSpriteStorageKey = previewSpriteStorageKey,
            SortOrder = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var playbackUrlBuilder = new TestPlaybackUrlBuilder();
        playbackUrlBuilder.ExistingStorageKeys.Add(previewVttStorageKey);

        var handler = new GetAdminWorkByIdQueryHandler(CreateWorkQueryStore(dbContext, playbackUrlBuilder));
        var result = await handler.Handle(new(workId), CancellationToken.None);

        Assert.NotNull(result);
        var video = Assert.Single(result!.Videos);
        Assert.Null(video.TimelinePreviewVttUrl);
        Assert.Null(video.TimelinePreviewSpriteUrl);
        Assert.Equal(2, playbackUrlBuilder.StorageObjectExistsCallCount);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_ReturnsPagedResults()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.AddRange(
            new Blog { Id = Guid.NewGuid(), Title = "Blog A", Slug = "blog-a", Excerpt = "a", ContentJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow.AddDays(-1), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Blog { Id = Guid.NewGuid(), Title = "Blog B", Slug = "blog-b", Excerpt = "b", ContentJson = "{}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreateBlogQueryStore(dbContext));

        var result = await handler.Handle(new GetBlogsQuery(Page: 2, PageSize: 1), CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_ReturnsStableEmptyPage_WhenNoBlogsArePublished()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.Add(CreateBlog("Draft Blog", "draft-blog", published: false, publishedAt: null));
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreateBlogQueryStore(dbContext));

        var result = await handler.Handle(new GetBlogsQuery(Page: 3, PageSize: 2), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_FiltersDraftsOrdersByPublishedAtAndMapsStoredCoverUrl()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        var coverAssetId = Guid.NewGuid();
        var draftCoverAssetId = Guid.NewGuid();
        dbContext.Assets.AddRange(
            new Asset { Id = coverAssetId, Bucket = "media", Path = "published-cover.png", PublicUrl = "/media/published-cover.png" },
            new Asset { Id = draftCoverAssetId, Bucket = "media", Path = "draft-cover.png", PublicUrl = "/media/draft-cover.png" });
        dbContext.Blogs.AddRange(
            CreateBlog(
                "Older Blog",
                "older-blog",
                published: true,
                publishedAt: now.AddDays(-3),
                coverAssetId,
                publicCoverUrl: "/media/published-cover.png"),
            CreateBlog("Newer Blog", "newer-blog", published: true, publishedAt: now.AddDays(-1)),
            CreateBlog("Draft Blog", "draft-blog", published: false, publishedAt: now, draftCoverAssetId));
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreateBlogQueryStore(dbContext));

        var result = await handler.Handle(new GetBlogsQuery(Page: 1, PageSize: 10), CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(new[] { "Newer Blog", "Older Blog" }, result.Items.Select(x => x.Title).ToArray());
        Assert.DoesNotContain(result.Items, x => x.Slug == "draft-blog");
        Assert.Equal("/media/published-cover.png", result.Items[1].CoverUrl);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_ClampsRequestedPageBeyondLastPage()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        dbContext.Blogs.AddRange(
            CreateBlog("Older Blog", "older-blog", published: true, publishedAt: now.AddDays(-2)),
            CreateBlog("Newer Blog", "newer-blog", published: true, publishedAt: now.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreateBlogQueryStore(dbContext));

        var result = await handler.Handle(new GetBlogsQuery(Page: 99, PageSize: 1), CancellationToken.None);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("Older Blog", result.Items[0].Title);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_FiltersByNormalizedTitleSearch()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.AddRange(
            new Blog { Id = Guid.NewGuid(), Title = "T,B,N 안녕하세요", Slug = "tbn-blog", Excerpt = "alpha", ContentJson = "{\"html\":\"<p>alpha</p>\"}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Blog { Id = Guid.NewGuid(), Title = "Different Blog", Slug = "different-blog", Excerpt = "beta", ContentJson = "{\"html\":\"<p>beta</p>\"}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreateBlogQueryStore(dbContext));

        var result = await handler.Handle(new GetBlogsQuery(Query: "TBN", SearchMode: "title"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("T,B,N 안녕하세요", result.Items[0].Title);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_QueryOnly_PerformsUnifiedSearch()
    {
        await using var dbContext = CreateDbContext();
        var bodyToken = $"blog-body-token-{Guid.NewGuid():N}";
        dbContext.Blogs.AddRange(
            new Blog { Id = Guid.NewGuid(), Title = "Unified Blog Match", Slug = "unified-blog-match", Excerpt = "alpha", ContentJson = "{\"html\":\"<p>alpha</p>\"}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Blog { Id = Guid.NewGuid(), Title = "No title token", Slug = "unified-blog-content", Excerpt = $"contains {bodyToken}", ContentJson = "{\"html\":\"<p>beta</p>\"}", Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreateBlogQueryStore(dbContext));

        var titleResult = await handler.Handle(new GetBlogsQuery(Query: "unified blog"), CancellationToken.None);
        var contentResult = await handler.Handle(new GetBlogsQuery(Query: bodyToken), CancellationToken.None);

        Assert.Single(titleResult.Items);
        Assert.Equal("Unified Blog Match", titleResult.Items[0].Title);
        Assert.Single(contentResult.Items);
        Assert.Equal("No title token", contentResult.Items[0].Title);
    }

    [Fact]
    public async Task GetBlogBySlugQueryHandler_ReturnsPublishedDetailWithStoredCoverUrl()
    {
        await using var dbContext = CreateDbContext();
        var coverAssetId = Guid.NewGuid();
        var publishedAt = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        dbContext.Blogs.Add(CreateBlog(
            "Public Blog Detail",
            "public-blog-detail",
            published: true,
            publishedAt,
            coverAssetId,
            publicCoverUrl: "/media/detail-cover.png"));
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogBySlugQueryHandler(CreateBlogQueryStore(dbContext));

        var result = await handler.Handle(new GetBlogBySlugQuery("public-blog-detail"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("public-blog-detail", result!.Slug);
        Assert.Equal("Public Blog Detail", result.Title);
        Assert.Equal("Excerpt for Public Blog Detail", result.Excerpt);
        Assert.Contains("Public Blog Detail body", result.Content.Html, StringComparison.Ordinal);
        Assert.Equal(new[] { "public", "test" }, result.Tags);
        Assert.Equal("/media/detail-cover.png", result.CoverUrl);
        Assert.Equal(publishedAt, result.PublishedAt);
    }

    [Fact]
    public async Task GetBlogBySlugQueryHandler_ReturnsNull_ForDraftOrMissingSlug()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.Add(CreateBlog("Draft Blog", "draft-blog", published: false, publishedAt: null));
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogBySlugQueryHandler(CreateBlogQueryStore(dbContext));

        var draftResult = await handler.Handle(new GetBlogBySlugQuery("draft-blog"), CancellationToken.None);
        var missingResult = await handler.Handle(new GetBlogBySlugQuery("missing-blog"), CancellationToken.None);

        Assert.Null(draftResult);
        Assert.Null(missingResult);
    }

    private static Blog CreateBlog(
        string title,
        string slug,
        bool published,
        DateTimeOffset? publishedAt,
        Guid? coverAssetId = null,
        string publicCoverUrl = "")
    {
        var timestamp = publishedAt ?? new DateTimeOffset(2026, 4, 25, 0, 0, 0, TimeSpan.Zero);

        return new Blog
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Excerpt = $"Excerpt for {title}",
            Tags = ["public", "test"],
            CoverAssetId = coverAssetId,
            PublicCoverUrl = publicCoverUrl,
            ContentJson = $$"""{"html":"<p>{{title}} body</p>"}""",
            Published = published,
            PublishedAt = publishedAt,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    private static Work CreateWork(
        string title,
        string slug,
        bool published,
        DateTimeOffset? publishedAt,
        Guid? thumbnailAssetId = null,
        Guid? iconAssetId = null,
        string publicThumbnailUrl = "",
        string publicIconUrl = "")
    {
        var timestamp = publishedAt ?? new DateTimeOffset(2026, 4, 25, 0, 0, 0, TimeSpan.Zero);

        return new Work
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Excerpt = $"Excerpt for {title}",
            Category = "public",
            Period = "2026",
            Tags = ["public", "test"],
            ThumbnailAssetId = thumbnailAssetId,
            IconAssetId = iconAssetId,
            PublicThumbnailUrl = publicThumbnailUrl,
            PublicIconUrl = publicIconUrl,
            ContentJson = $$"""{"html":"<p>{{title}} body</p>"}""",
            AllPropertiesJson = "{}",
            Published = published,
            PublishedAt = publishedAt,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }
}
