using WoongBlog.Api.Application.Public.GetWorkBySlug;
using WoongBlog.Api.Application.Public.GetWorks;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicWorkService
{
    Task<PagedWorksDto> GetWorksAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<WorkDetailDto?> GetWorkBySlugAsync(string slug, CancellationToken cancellationToken);
}
