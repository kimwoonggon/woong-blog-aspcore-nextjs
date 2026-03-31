using WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

namespace WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

public interface IPublicWorkService
{
    Task<PagedWorksDto> GetWorksAsync(GetWorksQuery query, CancellationToken cancellationToken);
    Task<WorkDetailDto?> GetWorkBySlugAsync(string slug, CancellationToken cancellationToken);
}
