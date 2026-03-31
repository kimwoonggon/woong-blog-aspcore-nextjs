using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.CreateWork;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorkById;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;
using WoongBlog.Api.Modules.Content.Works.Application.UpdateWork;

namespace WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

public interface IAdminWorkService
{
    Task<IReadOnlyList<AdminWorkListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminWorkDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMutationResult> CreateAsync(CreateWorkCommand command, CancellationToken cancellationToken);
    Task<AdminMutationResult?> UpdateAsync(UpdateWorkCommand command, CancellationToken cancellationToken);
    Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
