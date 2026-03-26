using Portfolio.Api.Application.Admin.GetAdminMembers;

namespace Portfolio.Api.Application.Admin.Abstractions;

public interface IAdminMemberService
{
    Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken);
}
