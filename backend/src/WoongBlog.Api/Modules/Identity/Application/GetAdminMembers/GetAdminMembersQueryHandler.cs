using MediatR;
using WoongBlog.Api.Modules.Identity.Application.Abstractions;

namespace WoongBlog.Api.Modules.Identity.Application.GetAdminMembers;

public sealed class GetAdminMembersQueryHandler : IRequestHandler<GetAdminMembersQuery, IReadOnlyList<AdminMemberListItemDto>>
{
    private readonly IAdminMemberService _adminMemberService;

    public GetAdminMembersQueryHandler(IAdminMemberService adminMemberService)
    {
        _adminMemberService = adminMemberService;
    }

    public async Task<IReadOnlyList<AdminMemberListItemDto>> Handle(GetAdminMembersQuery request, CancellationToken cancellationToken)
    {
        return await _adminMemberService.GetAllAsync(cancellationToken);
    }
}
