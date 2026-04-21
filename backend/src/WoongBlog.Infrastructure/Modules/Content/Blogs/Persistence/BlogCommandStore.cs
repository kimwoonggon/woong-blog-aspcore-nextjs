using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Persistence;

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
