using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminWorkById;
using WoongBlog.Api.Application.Admin.GetAdminWorks;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminWorkPersistence : IAdminWorkQueries, IAdminWorkWriteStore
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminWorkPersistence(WoongBlogDbContext dbContext)
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

    public Task<Work?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Works.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        return _dbContext.Works.AnyAsync(
            x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public void Add(Work work)
    {
        _dbContext.Works.Add(work);
    }

    public void Remove(Work work)
    {
        _dbContext.Works.Remove(work);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var url)
            ? url
            : string.Empty;
    }
}
