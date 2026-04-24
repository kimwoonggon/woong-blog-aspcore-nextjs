using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.Abstractions;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.Support;

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
        var assets = await dbContext.Assets.AsNoTracking().ToListAsync(cancellationToken);
        var assetById = assets.ToDictionary(x => x.Id, x => x);
        var assetPublicUrlById = assets.ToDictionary(x => x.Id, x => x.PublicUrl);

        var featuredWorks = await dbContext.Works
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(3)
            .ToListAsync(cancellationToken);
        var featuredWorkIds = featuredWorks.Select(work => work.Id).ToArray();
        var featuredWorkVideoRows = await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => featuredWorkIds.Contains(x.WorkId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var featuredWorkVideos = featuredWorkVideoRows
            .GroupBy(x => x.WorkId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<WorkVideo>)x.ToList());

        return featuredWorks.Select(work => new WorkCardDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.Category,
            work.Period,
            work.Tags,
            WorkThumbnailUrlResolver.ResolveThumbnailUrl(
                work.ThumbnailAssetId,
                work.ContentJson,
                featuredWorkVideos.TryGetValue(work.Id, out var workVideos) ? workVideos : Array.Empty<WorkVideo>(),
                assetPublicUrlById),
            ResolveAssetUrl(work.IconAssetId, assetById),
            work.PublishedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<BlogCardDto>> GetRecentPostsAsync(CancellationToken cancellationToken)
    {
        var assets = await dbContext.Assets.AsNoTracking().ToListAsync(cancellationToken);
        var assetById = assets.ToDictionary(x => x.Id, x => x);
        var recentPosts = await dbContext.Blogs
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        return recentPosts.Select(post => new BlogCardDto(
            post.Id,
            post.Slug,
            post.Title,
            post.Excerpt,
            post.Tags,
            ResolveAssetUrl(post.CoverAssetId, assetById),
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

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, Asset> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var asset)
            ? asset.PublicUrl
            : string.Empty;
    }
}
