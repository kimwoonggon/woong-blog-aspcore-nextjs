using WoongBlog.Api.Modules.Identity.Application.GetAdminMembers;

namespace WoongBlog.Api.Modules.Identity.Application.Abstractions;

public interface IAdminMemberQueryStore
{
    Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken);
}
