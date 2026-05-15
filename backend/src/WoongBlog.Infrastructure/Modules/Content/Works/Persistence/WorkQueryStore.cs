using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Common;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Abstractions;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorkById;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorks;
using WoongBlog.Application.Modules.Content.Works.GetWorkDetailContext;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.Support;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.Persistence;

public sealed class WorkQueryStore(
    WoongBlogDbContext dbContext,
    IWorkVideoPlaybackUrlBuilder playbackUrlBuilder) : IWorkQueryStore
{
    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const int MaxDetailContextLimit = 24;
    private static readonly ConcurrentDictionary<IModel, Func<WoongBlogDbContext, string, IAsyncEnumerable<PublicWorkDetailRow>>> PublishedDetailBySlugQueries = new();

    private static Func<WoongBlogDbContext, string, IAsyncEnumerable<PublicWorkDetailRow>> CreatePublishedDetailBySlugQuery(IModel _)
    {
        return EF.CompileAsyncQuery((WoongBlogDbContext context, string slug) =>
            context.Works.AsNoTracking()
                .Where(x => x.Slug == slug && x.Published)
                .Select(work => new PublicWorkDetailRow(
                    work.Id,
                    work.Slug,
                    work.Title,
                    work.Excerpt,
                    work.PublicContentHtml,
                    work.PublicContentMarkdown,
                    work.Category,
                    work.Period,
                    work.Tags,
                    work.PublicThumbnailUrl,
                    work.PublishedAt,
                    work.PublicSocialShareMessage,
                    work.VideosVersion,
                    work.PublicVideosJson))
                .Take(1));
    }

    public async Task<IReadOnlyList<AdminWorkListItemDto>> GetAdminListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Works
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminWorkListItemDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Excerpt,
                x.Category,
                x.Period,
                x.Tags,
                x.PublicThumbnailUrl,
                x.Published,
                x.PublishedAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminWorkDetailDto?> GetAdminDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var work = await dbContext.Works
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return work is null
            ? null
            : await BuildAdminDetailAsync(work, cancellationToken);
    }

    public async Task<PagedWorksDto> GetPublishedPageAsync(
        int page,
        int pageSize,
        string? normalizedQuery,
        ContentSearchMode searchMode,
        CancellationToken cancellationToken)
    {
        if (ShouldUsePostgresFirstPageWindowQuery(page, normalizedQuery))
        {
            return await GetPublishedFirstPageWithWindowAsync(pageSize, cancellationToken);
        }

        var query = ApplySearch(
                dbContext.Works.AsNoTracking().Where(x => x.Published),
                normalizedQuery,
                searchMode)
            .OrderByDescending(x => x.PublishedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var resolvedPage = Math.Min(page, totalPages);

        var works = await query
            .Skip((resolvedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(work => new WorkCardRow(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Tags,
                work.PublicThumbnailUrl,
                work.PublishedAt))
            .ToListAsync(cancellationToken);

        var items = works.Select(work => new WorkCardDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.Category,
            work.Tags,
            work.PublicThumbnailUrl,
            work.PublishedAt)).ToList();

        return new PagedWorksDto(items, resolvedPage, pageSize, totalItems, totalPages);
    }

    private bool ShouldUsePostgresFirstPageWindowQuery(int page, string? normalizedQuery)
    {
        return page == 1
            && normalizedQuery is null
            && string.Equals(dbContext.Database.ProviderName, PostgresProviderName, StringComparison.Ordinal);
    }

    private async Task<PagedWorksDto> GetPublishedFirstPageWithWindowAsync(
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database
            .SqlQuery<WorkCardWithTotalRow>(
                $"""
                SELECT
                    w."Id",
                    w."Slug",
                    w."Title",
                    w."Excerpt",
                    w."Category",
                    w."Tags",
                    w."PublicThumbnailUrl" AS "ThumbnailUrl",
                    w."PublishedAt",
                    COUNT(*) OVER()::integer AS "TotalItems"
                FROM "Works" AS w
                WHERE w."Published" = TRUE
                ORDER BY w."PublishedAt" DESC
                LIMIT {pageSize}
                """)
            .ToListAsync(cancellationToken);

        var totalItems = rows.Count == 0 ? 0 : rows[0].TotalItems;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var items = rows.Select(work => new WorkCardDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.Category,
            work.Tags,
            work.ThumbnailUrl,
            work.PublishedAt)).ToList();

        return new PagedWorksDto(items, 1, pageSize, totalItems, totalPages);
    }

    public async Task<WorkDetailDto?> GetPublishedDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        PublicWorkDetailRow? work = null;
        var query = PublishedDetailBySlugQueries.GetOrAdd(dbContext.Model, CreatePublishedDetailBySlugQuery);
        await foreach (var row in query(dbContext, slug).WithCancellation(cancellationToken))
        {
            work = row;
            break;
        }

        return work is null
            ? null
            : BuildPublicDetail(work);
    }

    public async Task<WorkDetailContextDto?> GetPublishedDetailContextBySlugAsync(
        string slug,
        int limit,
        CancellationToken cancellationToken)
    {
        var current = await dbContext.Works
            .AsNoTracking()
            .Where(work => work.Slug == slug && work.Published)
            .Select(work => new WorkContextCurrentRow(work.Id, work.Title, work.PublishedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return null;
        }

        var relatedLimit = Math.Clamp(limit, 0, MaxDetailContextLimit);
        var publishedWorks = dbContext.Works
            .AsNoTracking()
            .Where(work => work.Published && work.Id != current.Id);
        var currentPublishedAt = current.PublishedAt ?? DateTimeOffset.MinValue;

        var newer = await publishedWorks
            .Where(work =>
                (work.PublishedAt ?? DateTimeOffset.MinValue) > currentPublishedAt
                || ((work.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(work.Title, current.Title) < 0))
            .OrderBy(work => work.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(work => work.Title)
            .Select(work => new WorkCardDto(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Tags,
                work.PublicThumbnailUrl,
                work.PublishedAt))
            .FirstOrDefaultAsync(cancellationToken);

        var older = await publishedWorks
            .Where(work =>
                (work.PublishedAt ?? DateTimeOffset.MinValue) < currentPublishedAt
                || ((work.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(work.Title, current.Title) > 0))
            .OrderByDescending(work => work.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenBy(work => work.Title)
            .Select(work => new WorkCardDto(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Tags,
                work.PublicThumbnailUrl,
                work.PublishedAt))
            .FirstOrDefaultAsync(cancellationToken);

        var newerRelated = await publishedWorks
            .Where(work =>
                (work.PublishedAt ?? DateTimeOffset.MinValue) > currentPublishedAt
                || ((work.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(work.Title, current.Title) < 0))
            .OrderBy(work => work.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(work => work.Title)
            .Take(relatedLimit)
            .Select(work => new WorkCardDto(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Tags,
                work.PublicThumbnailUrl,
                work.PublishedAt))
            .ToListAsync(cancellationToken);

        var olderRelated = await publishedWorks
            .Where(work =>
                (work.PublishedAt ?? DateTimeOffset.MinValue) < currentPublishedAt
                || ((work.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(work.Title, current.Title) > 0))
            .OrderByDescending(work => work.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenBy(work => work.Title)
            .Take(relatedLimit)
            .Select(work => new WorkCardDto(
                work.Id,
                work.Slug,
                work.Title,
                work.Excerpt,
                work.Category,
                work.Tags,
                work.PublicThumbnailUrl,
                work.PublishedAt))
            .ToListAsync(cancellationToken);

        var related = BuildBalancedRelatedContext(newerRelated, olderRelated, relatedLimit);

        return new WorkDetailContextDto(newer, older, related);
    }

    private static List<WorkCardDto> BuildBalancedRelatedContext(
        List<WorkCardDto> newerClosest,
        List<WorkCardDto> olderClosest,
        int limit)
    {
        if (limit <= 0)
        {
            return [];
        }

        var newerTarget = limit / 2;
        var olderTarget = limit - newerTarget;
        var selectedNewer = newerClosest.Take(newerTarget).ToList();
        var selectedOlder = olderClosest.Take(olderTarget).ToList();

        var remaining = limit - selectedNewer.Count - selectedOlder.Count;
        if (remaining > 0)
        {
            selectedOlder.AddRange(olderClosest.Skip(selectedOlder.Count).Take(remaining));
        }

        remaining = limit - selectedNewer.Count - selectedOlder.Count;
        if (remaining > 0)
        {
            selectedNewer.AddRange(newerClosest.Skip(selectedNewer.Count).Take(remaining));
        }

        selectedNewer.Reverse();
        selectedNewer.AddRange(selectedOlder);
        return selectedNewer;
    }

    private async Task<AdminWorkDetailDto> BuildAdminDetailAsync(Work work, CancellationToken cancellationToken)
    {
        var assetLookup = await GetAssetLookupAsync(work.ThumbnailAssetId, work.IconAssetId, cancellationToken);
        var videoRows = await GetVideosAsync(work.Id, cancellationToken);
        var videos = await BuildVideoDtosAsync(videoRows, verifyPreviewAssets: true, cancellationToken);
        var thumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            work.ThumbnailAssetId,
            work.ContentJson,
            videoRows,
            assetLookup);

        return new AdminWorkDetailDto(
            work.Id,
            work.Title,
            work.Slug,
            work.Excerpt,
            work.Category,
            work.Period,
            work.Tags,
            work.Published,
            work.PublishedAt,
            work.UpdatedAt,
            work.VideosVersion,
            AdminContentJson.ParseObject(work.AllPropertiesJson),
            new AdminWorkContentDto(AdminContentJson.ExtractHtml(work.ContentJson)),
            work.ThumbnailAssetId,
            work.IconAssetId,
            thumbnailUrl,
            ResolveAssetUrl(work.IconAssetId, assetLookup),
            videos);
    }

    private WorkDetailDto BuildPublicDetail(PublicWorkDetailRow work)
    {
        var videoRows = WorkPublicVideosReadModel.Deserialize(work.PublicVideosJson);
        var videos = videoRows
            .Select(BuildPublicVideoDto)
            .ToList();

        return new WorkDetailDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            PublicContentBodyDto.FromStoredFields(work.PublicContentHtml, work.PublicContentMarkdown),
            work.Category,
            work.Period,
            work.Tags,
            work.ThumbnailUrl,
            work.PublishedAt,
            NormalizeOptional(work.PublicSocialShareMessage),
            work.VideosVersion,
            videos);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkVideo>>> GetVideoLookupAsync(
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

    private async Task<IReadOnlyDictionary<Guid, string>> GetAssetLookupAsync(
        Guid? thumbnailAssetId,
        Guid? iconAssetId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Assets.AsNoTracking()
            .Where(x => x.Id == thumbnailAssetId || x.Id == iconAssetId)
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
    }

    private async Task<IReadOnlyList<WorkVideo>> GetVideosAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<WorkVideoDto>> BuildVideoDtosAsync(
        IEnumerable<WorkVideo> videoRows,
        bool verifyPreviewAssets,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkVideoDto>();

        foreach (var video in videoRows)
        {
            var (previewStorageType, hasPreviewStorageType) = ResolvePreviewStorageType(video.SourceType, video.SourceKey);
            var hasPreviewAssets = hasPreviewStorageType
                && !string.IsNullOrWhiteSpace(video.TimelinePreviewVttStorageKey)
                && !string.IsNullOrWhiteSpace(video.TimelinePreviewSpriteStorageKey)
                && (!verifyPreviewAssets
                    || await PreviewAssetsExistAsync(
                        previewStorageType,
                        video.TimelinePreviewVttStorageKey!,
                        video.TimelinePreviewSpriteStorageKey!,
                        cancellationToken));

            results.Add(new WorkVideoDto(
                video.Id,
                video.SourceType,
                video.SourceKey,
                playbackUrlBuilder.BuildPlaybackUrl(video.SourceType, video.SourceKey),
                video.OriginalFileName,
                video.MimeType,
                video.FileSize,
                video.Width,
                video.Height,
                video.DurationSeconds,
                hasPreviewAssets ? playbackUrlBuilder.BuildStorageObjectUrl(previewStorageType, video.TimelinePreviewVttStorageKey!) : null,
                hasPreviewAssets ? playbackUrlBuilder.BuildStorageObjectUrl(previewStorageType, video.TimelinePreviewSpriteStorageKey!) : null,
                video.SortOrder,
                video.CreatedAt));
        }

        return results;
    }

    private PublicWorkVideoDto BuildPublicVideoDto(WorkPublicVideoSnapshot video)
    {
        var (previewStorageType, hasPreviewStorageType) = ResolvePreviewStorageType(video.SourceType, video.SourceKey);
        var hasPreviewAssets = hasPreviewStorageType
            && !string.IsNullOrWhiteSpace(video.TimelinePreviewVttStorageKey)
            && !string.IsNullOrWhiteSpace(video.TimelinePreviewSpriteStorageKey);

        return new PublicWorkVideoDto(
            video.Id,
            video.SourceType,
            video.SourceKey,
            playbackUrlBuilder.BuildPlaybackUrl(video.SourceType, video.SourceKey),
            video.MimeType,
            video.Width,
            video.Height,
            video.DurationSeconds,
            hasPreviewAssets ? playbackUrlBuilder.BuildStorageObjectUrl(previewStorageType, video.TimelinePreviewVttStorageKey!) : null,
            hasPreviewAssets ? playbackUrlBuilder.BuildStorageObjectUrl(previewStorageType, video.TimelinePreviewSpriteStorageKey!) : null,
            video.SortOrder);
    }

    private async Task<bool> PreviewAssetsExistAsync(
        string previewStorageType,
        string vttStorageKey,
        string spriteStorageKey,
        CancellationToken cancellationToken)
    {
        var vttExistsTask = playbackUrlBuilder.StorageObjectExistsAsync(previewStorageType, vttStorageKey, cancellationToken);
        var spriteExistsTask = playbackUrlBuilder.StorageObjectExistsAsync(previewStorageType, spriteStorageKey, cancellationToken);

        var vttExists = await vttExistsTask;
        var spriteExists = await spriteExistsTask;

        return vttExists && spriteExists;
    }

    private static (string StorageType, bool IsResolved) ResolvePreviewStorageType(string sourceType, string sourceKey)
    {
        if (string.Equals(sourceType, WorkVideoSourceTypes.Hls, StringComparison.OrdinalIgnoreCase)
            && WorkVideoHlsSourceKey.TryParse(sourceKey, out var storageType, out _))
        {
            return (storageType, true);
        }

        return string.IsNullOrWhiteSpace(sourceType)
            ? (string.Empty, false)
            : (sourceType, true);
    }

    private static IQueryable<Work> ApplySearch(
        IQueryable<Work> query,
        string? normalizedQuery,
        ContentSearchMode searchMode)
    {
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return query;
        }

        return searchMode switch
        {
            ContentSearchMode.Title => query.Where(x => x.SearchTitle.Contains(normalizedQuery)),
            ContentSearchMode.Content => query.Where(x => x.SearchText.Contains(normalizedQuery)),
            _ => query.Where(x => x.SearchTitle.Contains(normalizedQuery) || x.SearchText.Contains(normalizedQuery))
        };
    }

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var url)
            ? url
            : string.Empty;
    }

    private static string? NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed record WorkCardRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string Category,
        string[] Tags,
        string PublicThumbnailUrl,
        DateTimeOffset? PublishedAt);

    private sealed class WorkCardWithTotalRow
    {
        public Guid Id { get; init; }
        public string Slug { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string[] Tags { get; init; } = [];
        public string ThumbnailUrl { get; init; } = string.Empty;
        public DateTimeOffset? PublishedAt { get; init; }
        public int TotalItems { get; init; }
    }

    private sealed record PublicWorkDetailRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string PublicContentHtml,
        string PublicContentMarkdown,
        string Category,
        string? Period,
        string[] Tags,
        string ThumbnailUrl,
        DateTimeOffset? PublishedAt,
        string PublicSocialShareMessage,
        int VideosVersion,
        string PublicVideosJson);

    private sealed record WorkContextCurrentRow(
        Guid Id,
        string Title,
        DateTimeOffset? PublishedAt);
}
