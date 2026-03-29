using WoongBlog.Api.Application.Public.GetWorkBySlug;
using WoongBlog.Api.Application.Public.GetWorks;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicWorkService
{
    Task<PagedWorksDto> GetWorksAsync(GetWorksQuery query, CancellationToken cancellationToken);
    Task<WorkDetailDto?> GetWorkBySlugAsync(string slug, CancellationToken cancellationToken);
}
