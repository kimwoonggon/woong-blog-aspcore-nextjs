using WoongBlog.Api.Application.Admin.GetAdminBlogById;
using WoongBlog.Api.Application.Admin.GetAdminBlogs;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminBlogQueries
{
    Task<IReadOnlyList<AdminBlogListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminBlogDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAdminBlogWriteStore
{
    Task<Blog?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    void Add(Blog blog);
    void Remove(Blog blog);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
