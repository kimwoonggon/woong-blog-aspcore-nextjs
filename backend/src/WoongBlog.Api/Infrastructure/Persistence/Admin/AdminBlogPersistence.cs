using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminBlogById;
using WoongBlog.Api.Application.Admin.GetAdminBlogs;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminBlogPersistence : IAdminBlogQueries, IAdminBlogWriteStore
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminBlogPersistence(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminBlogListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Blogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminBlogListItemDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Excerpt,
                x.Tags,
                x.Published,
                x.PublishedAt,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminBlogDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Blogs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminBlogDetailDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Excerpt,
                x.Tags,
                x.Published,
                x.PublishedAt,
                x.UpdatedAt,
                new AdminBlogContentDto(AdminContentJson.ExtractHtml(x.ContentJson))
            ))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Blog?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Blogs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        return _dbContext.Blogs.AnyAsync(
            x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public void Add(Blog blog)
    {
        _dbContext.Blogs.Add(blog);
    }

    public void Remove(Blog blog)
    {
        _dbContext.Blogs.Remove(blog);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
