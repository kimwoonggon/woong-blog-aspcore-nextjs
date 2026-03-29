using WoongBlog.Api.Application.Public.GetPageBySlug;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicPageService
{
    Task<PageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken);
}
