using WoongBlog.Api.Application.Admin.CreateWork;
using WoongBlog.Api.Application.Admin.GetAdminWorkById;
using WoongBlog.Api.Application.Admin.GetAdminWorks;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Admin.UpdateWork;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminWorkService
{
    Task<IReadOnlyList<AdminWorkListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminWorkDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMutationResult> CreateAsync(CreateWorkCommand command, CancellationToken cancellationToken);
    Task<AdminMutationResult?> UpdateAsync(UpdateWorkCommand command, CancellationToken cancellationToken);
    Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
