using Portfolio.Api.Application.Public.GetPageBySlug;

namespace Portfolio.Api.Application.Public.Abstractions;

public interface IPublicPageService
{
    Task<PageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken);
}
