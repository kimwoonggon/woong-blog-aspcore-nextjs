using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

public interface IPublicBlogService
{
    Task<PagedBlogsDto> GetBlogsAsync(GetBlogsQuery query, CancellationToken cancellationToken);
    Task<BlogDetailDto?> GetBlogBySlugAsync(string slug, CancellationToken cancellationToken);
}
