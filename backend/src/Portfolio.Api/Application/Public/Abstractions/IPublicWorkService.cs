using Portfolio.Api.Application.Public.GetWorkBySlug;
using Portfolio.Api.Application.Public.GetWorks;

namespace Portfolio.Api.Application.Public.Abstractions;

public interface IPublicWorkService
{
    Task<PagedWorksDto> GetWorksAsync(GetWorksQuery query, CancellationToken cancellationToken);
    Task<WorkDetailDto?> GetWorkBySlugAsync(string slug, CancellationToken cancellationToken);
}
