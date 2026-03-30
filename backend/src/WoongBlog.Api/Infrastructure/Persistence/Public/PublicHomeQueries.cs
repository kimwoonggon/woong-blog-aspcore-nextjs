using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Public.GetHome;

namespace WoongBlog.Api.Infrastructure.Persistence.Public;

public sealed class PublicHomeQueries : IPublicHomeQueries
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicHomeQueries(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HomeDto?> GetHomeAsync(CancellationToken cancellationToken)
    {
        var siteSettings = await _dbContext.SiteSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Singleton, cancellationToken);
        var homePage = await _dbContext.Pages.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == "home", cancellationToken);

        if (siteSettings is null || homePage is null)
        {
            return null;
        }

        var assets = await _dbContext.Assets.AsNoTracking().ToListAsync(cancellationToken);
        var assetById = assets.ToDictionary(x => x.Id, x => x);

        var featuredWorks = await _dbContext.Works
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        var recentPosts = await _dbContext.Blogs
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(2)
            .ToListAsync(cancellationToken);

        return new HomeDto(
            new PageSummaryDto(homePage.Title, homePage.ContentJson),
            new SiteSettingsSummaryDto(
                siteSettings.OwnerName,
                siteSettings.Tagline,
                siteSettings.GitHubUrl,
                siteSettings.LinkedInUrl,
                ResolveAssetUrl(siteSettings.ResumeAssetId, assetById)
            ),
            featuredWorks.Select(work => new WorkCardDto(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Period,
                work.Tags,
                ResolveAssetUrl(work.ThumbnailAssetId, assetById),
                ResolveAssetUrl(work.IconAssetId, assetById),
                work.PublishedAt
            )).ToList(),
            recentPosts.Select(post => new BlogCardDto(
                post.Id,
                post.Slug,
                post.Title,
                post.Excerpt,
                post.Tags,
                ResolveAssetUrl(post.CoverAssetId, assetById),
                post.PublishedAt
            )).ToList()
        );
    }

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, Domain.Entities.Asset> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var asset)
            ? asset.PublicUrl
            : string.Empty;
    }
}
