using WoongBlog.Application.Modules.Content.Pages.GetAdminPages;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

namespace WoongBlog.Application.Modules.Content.Pages.Abstractions;

public interface IPageQueryStore
{
    Task<IReadOnlyList<AdminPageListItemDto>> GetAdminPagesAsync(string[]? slugs, CancellationToken cancellationToken);
    Task<PageDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
