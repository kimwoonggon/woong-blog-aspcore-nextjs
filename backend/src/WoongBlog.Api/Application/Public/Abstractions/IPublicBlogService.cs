using WoongBlog.Api.Application.Public.GetBlogBySlug;
using WoongBlog.Api.Application.Public.GetBlogs;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicBlogService
{
    Task<PagedBlogsDto> GetBlogsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<BlogDetailDto?> GetBlogBySlugAsync(string slug, CancellationToken cancellationToken);
}
