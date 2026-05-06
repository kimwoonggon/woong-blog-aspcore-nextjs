using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;

namespace WoongBlog.Infrastructure.Modules.Content.Blogs.Persistence;

public sealed class BlogCommandStore(WoongBlogDbContext dbContext) : IBlogCommandStore
{
    public Task<bool> SlugExistsAsync(string slug, Guid? excludedBlogId, CancellationToken cancellationToken)
    {
        return dbContext.Blogs.AnyAsync(
            x => x.Slug == slug && (!excludedBlogId.HasValue || x.Id != excludedBlogId.Value),
            cancellationToken);
    }

    public Task<Blog?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Blogs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetAssetPublicUrlsAsync(
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

    public void Add(Blog blog)
    {
        dbContext.Blogs.Add(blog);
    }

    public void Remove(Blog blog)
    {
        dbContext.Blogs.Remove(blog);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
