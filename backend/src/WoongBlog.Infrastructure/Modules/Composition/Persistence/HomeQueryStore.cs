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
                work.ThumbnailAssetId,
                work.IconAssetId,
                work.PublishedAt))
            .ToListAsync(cancellationToken);

        var featuredWorkIds = featuredWorks
            .Where(work => work.ThumbnailAssetId is null)
            .Select(work => work.Id)
            .ToArray();
        var featuredWorkVideos = await GetWorkVideoLookupAsync(featuredWorkIds, cancellationToken);
        var assetIds = featuredWorks
            .SelectMany(work => new[] { work.ThumbnailAssetId, work.IconAssetId })
            .Where(assetId => assetId.HasValue)
            .Select(assetId => assetId!.Value)
            .Distinct()
            .ToArray();
        var assetPublicUrlById = await GetAssetPublicUrlLookupAsync(assetIds, cancellationToken);

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
                contentJson: null,
                featuredWorkVideos.TryGetValue(work.Id, out var workVideos) ? workVideos : Array.Empty<WorkVideo>(),
                assetPublicUrlById),
            ResolveAssetUrl(work.IconAssetId, assetPublicUrlById),
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
                post.CoverAssetId,
                post.PublishedAt))
            .ToListAsync(cancellationToken);
        var assetIds = recentPosts
            .Where(post => post.CoverAssetId.HasValue)
            .Select(post => post.CoverAssetId!.Value)
            .Distinct()
            .ToArray();
        var assetPublicUrlById = await GetAssetPublicUrlLookupAsync(assetIds, cancellationToken);

        return recentPosts.Select(post => new BlogCardDto(
            post.Id,
            post.Slug,
            post.Title,
            post.Excerpt,
            post.Tags,
            ResolveAssetUrl(post.CoverAssetId, assetPublicUrlById),
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

    private async Task<IReadOnlyDictionary<Guid, string>> GetAssetPublicUrlLookupAsync(
        IReadOnlyCollection<Guid> assetIds,
        CancellationToken cancellationToken)
    {
        if (assetIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Assets
            .AsNoTracking()
            .Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkVideo>>> GetWorkVideoLookupAsync(
        IReadOnlyCollection<Guid> workIds,
        CancellationToken cancellationToken)
    {
        if (workIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<WorkVideo>>();
        }

        var videoRows = await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => workIds.Contains(x.WorkId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return videoRows
            .GroupBy(x => x.WorkId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<WorkVideo>)x.ToList());
    }

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assetPublicUrlById)
    {
        return assetId is not null && assetPublicUrlById.TryGetValue(assetId.Value, out var publicUrl)
            ? publicUrl
            : string.Empty;
    }

    private sealed record FeaturedWorkRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string Category,
        string? Period,
        string[] Tags,
        Guid? ThumbnailAssetId,
        Guid? IconAssetId,
        DateTimeOffset? PublishedAt);

    private sealed record RecentBlogRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string[] Tags,
        Guid? CoverAssetId,
        DateTimeOffset? PublishedAt);
}
