using Microsoft.EntityFrameworkCore;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.Abstractions;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Infrastructure.Modules.Composition.Persistence;

public sealed class HomeQueryStore(WoongBlogDbContext dbContext) : IHomeQueryStore
{
    public async Task<PageSummaryDto?> GetHomePageAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Pages
            .AsNoTracking()
            .Where(x => x.Slug == "home")
            .Select(x => new PageSummaryDto(x.Title, x.ContentJson))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SiteSettingsSummaryDto?> GetSiteSettingsSummaryAsync(CancellationToken cancellationToken)
    {
        var siteSettings = await dbContext.SiteSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Singleton, cancellationToken);

        if (siteSettings is null)
        {
            return null;
        }

        var resumePublicUrl = await ResolveAssetUrlAsync(siteSettings.ResumeAssetId, cancellationToken);
        return new SiteSettingsSummaryDto(
            siteSettings.OwnerName,
            siteSettings.Tagline,
            siteSettings.GitHubUrl,
            siteSettings.LinkedInUrl,
            resumePublicUrl);
    }

    public async Task<IReadOnlyList<WorkCardDto>> GetFeaturedWorksAsync(CancellationToken cancellationToken)
    {
        var featuredWorks = await dbContext.Works
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(3)
            .Select(work => new FeaturedWorkRow(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Period,
                work.Tags,
                work.PublicThumbnailUrl,
                work.PublicIconUrl,
                work.PublishedAt))
            .ToListAsync(cancellationToken);

        return featuredWorks.Select(work => new WorkCardDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.Category,
            work.Period,
            work.Tags,
            work.PublicThumbnailUrl,
            work.PublicIconUrl,
            work.PublishedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<BlogCardDto>> GetRecentPostsAsync(CancellationToken cancellationToken)
    {
        var recentPosts = await dbContext.Blogs
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(6)
            .Select(post => new RecentBlogRow(
                post.Id,
                post.Slug,
                post.Title,
                post.Excerpt,
                post.Tags,
                post.PublicCoverUrl,
                post.PublishedAt))
            .ToListAsync(cancellationToken);

        return recentPosts.Select(post => new BlogCardDto(
            post.Id,
            post.Slug,
            post.Title,
            post.Excerpt,
            post.Tags,
            post.PublicCoverUrl,
            post.PublishedAt
        )).ToList();
    }

    private async Task<string> ResolveAssetUrlAsync(Guid? assetId, CancellationToken cancellationToken)
    {
        if (assetId is null)
        {
            return string.Empty;
        }

        return await dbContext.Assets
            .AsNoTracking()
            .Where(x => x.Id == assetId.Value)
            .Select(x => x.PublicUrl)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    private sealed record FeaturedWorkRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string Category,
        string? Period,
        string[] Tags,
        string PublicThumbnailUrl,
        string PublicIconUrl,
        DateTimeOffset? PublishedAt);

    private sealed record RecentBlogRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string[] Tags,
        string PublicCoverUrl,
        DateTimeOffset? PublishedAt);
}
