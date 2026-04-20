using MediatR;
using WoongBlog.Api.Modules.Identity.Application.Abstractions;

namespace WoongBlog.Api.Modules.Identity.Application.GetAdminMembers;

public sealed class GetAdminMembersQueryHandler : IRequestHandler<GetAdminMembersQuery, IReadOnlyList<AdminMemberListItemDto>>
{
    private readonly IAdminMemberQueryStore _adminMemberQueryStore;

    public GetAdminMembersQueryHandler(IAdminMemberQueryStore adminMemberQueryStore)
    {
        _adminMemberQueryStore = adminMemberQueryStore;
    }

    public async Task<IReadOnlyList<AdminMemberListItemDto>> Handle(GetAdminMembersQuery request, CancellationToken cancellationToken)
    {
        return await _adminMemberQueryStore.GetAllAsync(cancellationToken);
    }
}
