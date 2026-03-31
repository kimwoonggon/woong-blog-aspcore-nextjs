using WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

namespace WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

public interface IPublicPageService
{
    Task<PageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken);
}
