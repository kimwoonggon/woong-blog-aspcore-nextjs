using WoongBlog.Api.Application.Public.GetPageBySlug;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicPageQueries
{
    Task<PageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken);
}
