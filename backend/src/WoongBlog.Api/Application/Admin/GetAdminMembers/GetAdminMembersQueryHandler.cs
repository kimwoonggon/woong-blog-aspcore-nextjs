using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminMembers;

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
