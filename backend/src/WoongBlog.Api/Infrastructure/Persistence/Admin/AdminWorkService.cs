using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.CreateWork;
using WoongBlog.Api.Application.Admin.GetAdminWorkById;
using WoongBlog.Api.Application.Admin.GetAdminWorks;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Admin.UpdateWork;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminWorkService : IAdminWorkService
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminWorkService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
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
            AdminContentJson.ParseObject(work.AllPropertiesJson),
            new AdminWorkContentDto(AdminContentJson.ExtractHtml(work.ContentJson)),
            work.ThumbnailAssetId,
            work.IconAssetId,
            ResolveAssetUrl(work.ThumbnailAssetId, assetLookup),
            ResolveAssetUrl(work.IconAssetId, assetLookup)
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
