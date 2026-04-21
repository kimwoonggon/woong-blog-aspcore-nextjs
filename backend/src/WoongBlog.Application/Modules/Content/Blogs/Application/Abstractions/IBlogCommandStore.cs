using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

public interface IBlogCommandStore
{
    Task<bool> SlugExistsAsync(string slug, Guid? excludedBlogId, CancellationToken cancellationToken);
    Task<Blog?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    void Add(Blog blog);
    void Remove(Blog blog);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
