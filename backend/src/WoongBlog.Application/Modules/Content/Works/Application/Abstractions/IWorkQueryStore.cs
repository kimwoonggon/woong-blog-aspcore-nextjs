using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorkById;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

namespace WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

public interface IWorkQueryStore
{
    Task<IReadOnlyList<AdminWorkListItemDto>> GetAdminListAsync(CancellationToken cancellationToken);
    Task<AdminWorkDetailDto?> GetAdminDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedWorksDto> GetPublishedPageAsync(
        int page,
        int pageSize,
        string? normalizedQuery,
        ContentSearchMode searchMode,
        CancellationToken cancellationToken);
    Task<WorkDetailDto?> GetPublishedDetailBySlugAsync(string slug, CancellationToken cancellationToken);
}
