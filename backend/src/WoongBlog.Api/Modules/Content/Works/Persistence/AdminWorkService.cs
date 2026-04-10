using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.CreateWork;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorkById;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;
using WoongBlog.Api.Modules.Content.Works.Application.UpdateWork;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Persistence;

public sealed class AdminWorkService : IAdminWorkService
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly IWorkVideoPlaybackUrlBuilder _playbackUrlBuilder;

    public AdminWorkService(WoongBlogDbContext dbContext, IWorkVideoPlaybackUrlBuilder playbackUrlBuilder)
    {
        _dbContext = dbContext;
        _playbackUrlBuilder = playbackUrlBuilder;
    }

    public async Task<IReadOnlyList<AdminWorkListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Works
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
                x.Published,
                x.PublishedAt,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminWorkDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (work is null)
        {
            return null;
        }

        var assetLookup = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.Id == work.ThumbnailAssetId || x.Id == work.IconAssetId)
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
        var videoRows = await _dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == work.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var videos = videoRows.Select(x => new WorkVideoDto(
            x.Id,
            x.SourceType,
            x.SourceKey,
            _playbackUrlBuilder.BuildPlaybackUrl(x.SourceType, x.SourceKey),
            x.OriginalFileName,
            x.MimeType,
            x.FileSize,
            x.SortOrder,
            x.CreatedAt
        )).ToList();

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
            ResolveAssetUrl(work.ThumbnailAssetId, assetLookup),
            ResolveAssetUrl(work.IconAssetId, assetLookup),
            videos
        );
    }

    public async Task<AdminMutationResult> CreateAsync(CreateWorkCommand command, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(command.Title, null, cancellationToken);
        var excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(command.ContentJson));
        var now = DateTimeOffset.UtcNow;

        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Slug = slug,
            Excerpt = excerpt,
            ThumbnailAssetId = command.ThumbnailAssetId,
            IconAssetId = command.IconAssetId,
            Category = command.Category,
            Period = command.Period,
            Tags = command.Tags,
            Published = command.Published,
            PublishedAt = command.Published ? now : null,
            ContentJson = command.ContentJson,
            AllPropertiesJson = command.AllPropertiesJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Works.Add(work);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(work.Id, work.Slug);
    }

    public async Task<AdminMutationResult?> UpdateAsync(UpdateWorkCommand command, CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (work is null)
        {
            return null;
        }

        work.Title = command.Title;
        work.Slug = await GenerateUniqueSlugAsync(command.Title, work.Id, cancellationToken);
        work.Excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(command.ContentJson));
        work.ThumbnailAssetId = command.ThumbnailAssetId;
        work.IconAssetId = command.IconAssetId;
        work.Category = command.Category;
        work.Period = command.Period;
        work.Tags = command.Tags;
        work.ContentJson = command.ContentJson;
        work.AllPropertiesJson = command.AllPropertiesJson;
        work.UpdatedAt = DateTimeOffset.UtcNow;
        work.Published = command.Published;
        if (command.Published && work.PublishedAt is null)
        {
            work.PublishedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(work.Id, work.Slug);
    }

    public async Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (work is null)
        {
            return new AdminActionResult(false);
        }

        var workVideos = await _dbContext.WorkVideos.Where(x => x.WorkId == id).ToListAsync(cancellationToken);
        var uploadSessions = await _dbContext.WorkVideoUploadSessions.Where(x => x.WorkId == id).ToListAsync(cancellationToken);

        if (workVideos.Count > 0)
        {
            _dbContext.WorkVideos.RemoveRange(workVideos);
        }

        if (uploadSessions.Count > 0)
        {
            _dbContext.WorkVideoUploadSessions.RemoveRange(uploadSessions);
        }

        _dbContext.Works.Remove(work);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, Guid? currentWorkId, CancellationToken cancellationToken)
    {
        var baseSlug = AdminContentText.Slugify(title, "work");
        var slug = baseSlug;
        var suffix = 2;

        while (await _dbContext.Works.AnyAsync(
                   x => x.Slug == slug && (!currentWorkId.HasValue || x.Id != currentWorkId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var url)
            ? url
            : string.Empty;
    }
}
