using WoongBlog.Application.Modules.Identity.GetAdminMembers;

namespace WoongBlog.Application.Modules.Identity.Abstractions;

public interface IAdminMemberQueryStore
{
    Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken);
}
