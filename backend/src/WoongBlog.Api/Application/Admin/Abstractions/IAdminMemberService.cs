using WoongBlog.Api.Application.Admin.GetAdminMembers;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminMemberService
{
    Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken);
}
