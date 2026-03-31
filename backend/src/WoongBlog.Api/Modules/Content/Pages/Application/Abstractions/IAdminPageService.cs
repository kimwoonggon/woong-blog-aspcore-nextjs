using WoongBlog.Api.Modules.Content.Pages.Application.GetAdminPages;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

public interface IAdminPageService
{
    Task<IReadOnlyList<AdminPageListItemDto>> GetPagesAsync(string[]? slugs, CancellationToken cancellationToken);
    Task<AdminActionResult> UpdatePageAsync(Guid id, string title, string contentJson, CancellationToken cancellationToken);
}
