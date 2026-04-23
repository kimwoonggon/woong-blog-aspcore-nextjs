using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Application.GetHome;
using WoongBlog.Api.Modules.Composition.Persistence;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;
using WoongBlog.Api.Modules.Content.Blogs.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;
using WoongBlog.Api.Modules.Content.Works.Persistence;
using WoongBlog.Api.Modules.Site.Application.Abstractions;
using WoongBlog.Api.Modules.Site.Application.GetResume;
using WoongBlog.Api.Modules.Site.Application.GetSiteSettings;
using WoongBlog.Api.Modules.Site.Persistence;

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
    private static IWorkQueryStore CreateWorkQueryStore(WoongBlogDbContext dbContext) => new WorkQueryStore(dbContext, new TestPlaybackUrlBuilder());
    private static IBlogQueryStore CreateBlogQueryStore(WoongBlogDbContext dbContext) => new BlogQueryStore(dbContext);

    private sealed class TestPlaybackUrlBuilder : IWorkVideoPlaybackUrlBuilder
    {
        public string? BuildPlaybackUrl(string sourceType, string sourceKey)
        {
            return sourceType == WorkVideoSourceTypes.YouTube ? null : $"/media/{sourceKey}";
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
    public async Task GetWorksQueryHandler_SkipsDrafts_AndResolvesAssetUrls()
    {
        await using var dbContext = CreateDbContext();
        var thumbnailId = Guid.NewGuid();
        dbContext.Assets.Add(new Asset { Id = thumbnailId, Bucket = "media", Path = "thumb.png", PublicUrl = "/media/thumb.png" });
        dbContext.Works.AddRange(
            new Work { Id = Guid.NewGuid(), Title = "Visible", Slug = "visible", Excerpt = "visible", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", ThumbnailAssetId = thumbnailId, Published = true, PublishedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Work { Id = Guid.NewGuid(), Title = "Hidden", Slug = "hidden", Excerpt = "hidden", Category = "cat", ContentJson = "{}", AllPropertiesJson = "{}", Published = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreateWorkQueryStore(dbContext));

        var result = await handler.Handle(new GetWorksQuery(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("/media/thumb.png", result.Items[0].ThumbnailUrl);
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
}
