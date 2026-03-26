using Portfolio.Api.Application.Admin.CreateWork;
using Portfolio.Api.Application.Admin.GetAdminWorkById;
using Portfolio.Api.Application.Admin.GetAdminWorks;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Application.Admin.UpdateWork;

namespace Portfolio.Api.Application.Admin.Abstractions;

public interface IAdminWorkService
{
    Task<IReadOnlyList<AdminWorkListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminWorkDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMutationResult> CreateAsync(CreateWorkCommand command, CancellationToken cancellationToken);
    Task<AdminMutationResult?> UpdateAsync(UpdateWorkCommand command, CancellationToken cancellationToken);
    Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
