using Portfolio.Api.Application.Public.GetBlogBySlug;
using Portfolio.Api.Application.Public.GetBlogs;

namespace Portfolio.Api.Application.Public.Abstractions;

public interface IPublicBlogService
{
    Task<PagedBlogsDto> GetBlogsAsync(GetBlogsQuery query, CancellationToken cancellationToken);
    Task<BlogDetailDto?> GetBlogBySlugAsync(string slug, CancellationToken cancellationToken);
}
