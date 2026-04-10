using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Application.GetHome;
using WoongBlog.Api.Modules.Content.Works.Persistence;

namespace WoongBlog.Api.Modules.Composition.Persistence;

public sealed class PublicHomeService : IPublicHomeService
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicHomeService(WoongBlogDbContext dbContext)
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
        var assetPublicUrlById = assets.ToDictionary(x => x.Id, x => x.PublicUrl);

        var featuredWorks = await _dbContext.Works
            .AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Take(3)
            .ToListAsync(cancellationToken);
        var featuredWorkIds = featuredWorks.Select(work => work.Id).ToArray();
        var featuredWorkVideoRows = await _dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => featuredWorkIds.Contains(x.WorkId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var featuredWorkVideos = featuredWorkVideoRows
            .GroupBy(x => x.WorkId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<WorkVideo>)x.ToList());

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
                WorkThumbnailUrlResolver.ResolveThumbnailUrl(
                    work.ThumbnailAssetId,
                    work.ContentJson,
                    featuredWorkVideos.TryGetValue(work.Id, out var workVideos) ? workVideos : Array.Empty<WorkVideo>(),
                    assetPublicUrlById),
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
