using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Public.GetHome;
using WoongBlog.Api.Infrastructure.Persistence.Assets;

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
        var siteSettings = await _dbContext.SiteSettings
            .AsNoTracking()
            .Where(x => x.Singleton)
            .Select(x => new
            {
                x.OwnerName,
                x.Tagline,
                x.GitHubUrl,
                x.LinkedInUrl,
                x.ResumeAssetId
            })
            .SingleOrDefaultAsync(cancellationToken);
        var homePage = await _dbContext.Pages
            .AsNoTracking()
            .Where(x => x.Slug == "home")
            .Select(x => new
            {
                x.Title,
                x.ContentJson
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (siteSettings is null || homePage is null)
        {
            return null;
        }

        var featuredWorks = await _dbContext.Works
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(3)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Title,
                x.Excerpt,
                x.Category,
                x.Period,
                x.Tags,
                x.ThumbnailAssetId,
                x.IconAssetId,
                x.PublishedAt
            })
            .ToListAsync(cancellationToken);

        var recentPosts = await _dbContext.Blogs
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(2)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Title,
                x.Excerpt,
                x.Tags,
                x.CoverAssetId,
                x.PublishedAt
            })
            .ToListAsync(cancellationToken);

        var assetById = await _dbContext.LoadPublicUrlLookupAsync(
            [siteSettings.ResumeAssetId,
             .. featuredWorks.SelectMany(work => new Guid?[] { work.ThumbnailAssetId, work.IconAssetId }),
             .. recentPosts.Select(post => post.CoverAssetId)],
            cancellationToken);

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

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var assetUrl)
            ? assetUrl
            : string.Empty;
    }
}
