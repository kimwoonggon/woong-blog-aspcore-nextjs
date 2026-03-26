using Portfolio.Api.Application.Admin.GetAdminPages;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.Abstractions;

public interface IAdminPageService
{
    Task<IReadOnlyList<AdminPageListItemDto>> GetPagesAsync(string[]? slugs, CancellationToken cancellationToken);
    Task<AdminActionResult> UpdatePageAsync(Guid id, string title, string contentJson, CancellationToken cancellationToken);
}
