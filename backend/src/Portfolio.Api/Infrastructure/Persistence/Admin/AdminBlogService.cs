using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Admin.Abstractions;
using Portfolio.Api.Application.Admin.CreateBlog;
using Portfolio.Api.Application.Admin.GetAdminBlogById;
using Portfolio.Api.Application.Admin.GetAdminBlogs;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Application.Admin.UpdateBlog;
using Portfolio.Api.Domain.Entities;

namespace Portfolio.Api.Infrastructure.Persistence.Admin;

public sealed class AdminBlogService : IAdminBlogService
{
    private readonly PortfolioDbContext _dbContext;

    public AdminBlogService(PortfolioDbContext dbContext)
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

    public async Task<AdminMutationResult> CreateAsync(CreateBlogCommand command, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(command.Title, null, cancellationToken);
        var excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(command.ContentJson));
        var now = DateTimeOffset.UtcNow;

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Slug = slug,
            Excerpt = excerpt,
            Tags = command.Tags,
            Published = command.Published,
            PublishedAt = command.Published ? now : null,
            ContentJson = command.ContentJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Blogs.Add(blog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(blog.Id, blog.Slug);
    }

    public async Task<AdminMutationResult?> UpdateAsync(UpdateBlogCommand command, CancellationToken cancellationToken)
    {
        var blog = await _dbContext.Blogs.SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (blog is null)
        {
            return null;
        }

        blog.Title = command.Title;
        blog.Slug = await GenerateUniqueSlugAsync(command.Title, blog.Id, cancellationToken);
        blog.Excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(command.ContentJson));
        blog.Tags = command.Tags;
        blog.ContentJson = command.ContentJson;
        blog.UpdatedAt = DateTimeOffset.UtcNow;
        blog.Published = command.Published;
        if (command.Published && blog.PublishedAt is null)
        {
            blog.PublishedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(blog.Id, blog.Slug);
    }

    public async Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var blog = await _dbContext.Blogs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (blog is null)
        {
            return new AdminActionResult(false);
        }

        _dbContext.Blogs.Remove(blog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, Guid? currentBlogId, CancellationToken cancellationToken)
    {
        var baseSlug = AdminContentText.Slugify(title, "post");
        var slug = baseSlug;
        var suffix = 2;

        while (await _dbContext.Blogs.AnyAsync(
                   x => x.Slug == slug && (!currentBlogId.HasValue || x.Id != currentBlogId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }
}
