using WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogById;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogs;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

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
