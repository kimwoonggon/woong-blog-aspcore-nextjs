using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Public.GetBlogs;
using WoongBlog.Api.Application.Public.GetHome;
using WoongBlog.Api.Application.Public.GetResume;
using WoongBlog.Api.Application.Public.GetSiteSettings;
using WoongBlog.Api.Application.Public.GetWorks;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Persistence.Public;

namespace WoongBlog.Api.Tests;

public class PublicQueryHandlerTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static IPublicHomeQueries CreatePublicHomeService(WoongBlogDbContext dbContext) => new PublicHomeQueries(dbContext);
    private static IPublicSiteQueries CreatePublicSiteService(WoongBlogDbContext dbContext) => new PublicSiteQueries(dbContext);
    private static IPublicWorkQueries CreatePublicWorkService(WoongBlogDbContext dbContext) => new PublicWorkQueries(dbContext);
    private static IPublicBlogQueries CreatePublicBlogService(WoongBlogDbContext dbContext) => new PublicBlogQueries(dbContext);

    [Fact]
    public async Task GetHomeQueryHandler_ReturnsNull_WhenHomePageMissing()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SiteSettings.Add(SiteSetting.Create("Owner", "Tagline", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreatePublicHomeService(dbContext));

        var result = await handler.Handle(new GetHomeQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHomeQueryHandler_ReturnsOnlyPublishedContent()
    {
        await using var dbContext = CreateDbContext();
        var resumeAssetId = Guid.NewGuid();
        dbContext.SiteSettings.Add(SiteSetting.Create("Owner", "Tagline", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, resumeAssetId, DateTimeOffset.UtcNow));
        dbContext.Assets.Add(Asset.Create("media", "resume.pdf", "/media/resume.pdf", string.Empty, "other", null, null, DateTimeOffset.UtcNow, resumeAssetId));
        dbContext.Pages.Add(PageEntity.Create("home", "Home", "{\"headline\":\"Hi\"}", Guid.NewGuid(), DateTimeOffset.UtcNow));
        dbContext.Works.AddRange(
            Work.Seed(new WorkUpsertValues("Published Work", "cat", null!, Array.Empty<string>(), true, "{}", "{}", null, null), "published-work", "published", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow),
            Work.Seed(new WorkUpsertValues("Draft Work", "cat", null!, Array.Empty<string>(), false, "{}", "{}", null, null), "draft-work", "draft", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid())
        );
        dbContext.Blogs.AddRange(
            Blog.Seed(new BlogUpsertValues("Published Blog", Array.Empty<string>(), true, "{}", null), "published-blog", "published", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow),
            Blog.Seed(new BlogUpsertValues("Draft Blog", Array.Empty<string>(), false, "{}", null), "draft-blog", "draft", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid())
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetHomeQueryHandler(CreatePublicHomeService(dbContext));

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
        dbContext.SiteSettings.Add(SiteSetting.Create("Owner", "Tagline", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();

        var handler = new GetResumeQueryHandler(CreatePublicSiteService(dbContext));

        var result = await handler.Handle(new GetResumeQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSiteSettingsQueryHandler_ReturnsNull_WhenSettingsMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetSiteSettingsQueryHandler(CreatePublicSiteService(dbContext));

        var result = await handler.Handle(new GetSiteSettingsQuery(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorksQueryHandler_SkipsDrafts_AndResolvesAssetUrls()
    {
        await using var dbContext = CreateDbContext();
        var thumbnailId = Guid.NewGuid();
        dbContext.Assets.Add(Asset.Create("media", "thumb.png", "/media/thumb.png", string.Empty, "other", null, null, DateTimeOffset.UtcNow, thumbnailId));
        dbContext.Works.AddRange(
            Work.Seed(new WorkUpsertValues("Visible", "cat", null!, Array.Empty<string>(), true, "{}", "{}", thumbnailId, null), "visible", "visible", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow),
            Work.Seed(new WorkUpsertValues("Hidden", "cat", null!, Array.Empty<string>(), false, "{}", "{}", null, null), "hidden", "hidden", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid())
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreatePublicWorkService(dbContext));

        var result = await handler.Handle(new GetWorksQuery(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("/media/thumb.png", result.Items[0].ThumbnailUrl);
    }

    [Fact]
    public async Task GetWorksQueryHandler_ReturnsPagedResults()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Works.AddRange(
            Work.Seed(new WorkUpsertValues("Work A", "cat", null!, Array.Empty<string>(), true, "{}", "{}", null, null), "work-a", "a", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1)),
            Work.Seed(new WorkUpsertValues("Work B", "cat", null!, Array.Empty<string>(), true, "{}", "{}", null, null), "work-b", "b", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow)
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetWorksQueryHandler(CreatePublicWorkService(dbContext));

        var result = await handler.Handle(new GetWorksQuery(Page: 2, PageSize: 1), CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetBlogsQueryHandler_ReturnsPagedResults()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.AddRange(
            Blog.Seed(new BlogUpsertValues("Blog A", Array.Empty<string>(), true, "{}", null), "blog-a", "a", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1)),
            Blog.Seed(new BlogUpsertValues("Blog B", Array.Empty<string>(), true, "{}", null), "blog-b", "b", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow)
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetBlogsQueryHandler(CreatePublicBlogService(dbContext));

        var result = await handler.Handle(new GetBlogsQuery(Page: 2, PageSize: 1), CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
    }
}
