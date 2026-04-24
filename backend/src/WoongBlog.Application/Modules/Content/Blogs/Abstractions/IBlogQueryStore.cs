using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogById;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogs;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Blogs.Abstractions;

public interface IBlogQueryStore
{
    Task<IReadOnlyList<AdminBlogListItemDto>> GetAdminListAsync(CancellationToken cancellationToken);
    Task<AdminBlogDetailDto?> GetAdminDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedBlogsDto> GetPublishedPageAsync(
        int page,
        int pageSize,
        string? normalizedQuery,
        ContentSearchMode searchMode,
        CancellationToken cancellationToken);
    Task<BlogDetailDto?> GetPublishedDetailBySlugAsync(string slug, CancellationToken cancellationToken);
}
