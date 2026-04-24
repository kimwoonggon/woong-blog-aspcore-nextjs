using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorkById;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorks;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Application.Modules.Content.Works.Abstractions;

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
