using WoongBlog.Api.Modules.Content.Pages.Application.GetAdminPages;
using WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

namespace WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

public interface IPageQueryStore
{
    Task<IReadOnlyList<AdminPageListItemDto>> GetAdminPagesAsync(string[]? slugs, CancellationToken cancellationToken);
    Task<PageDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
