using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminMembers;

public sealed class GetAdminMembersQueryHandler : IRequestHandler<GetAdminMembersQuery, IReadOnlyList<AdminMemberListItemDto>>
{
    private readonly IAdminMemberQueries _adminMemberQueries;

    public GetAdminMembersQueryHandler(IAdminMemberQueries adminMemberQueries)
    {
        _adminMemberQueries = adminMemberQueries;
    }

    public async Task<IReadOnlyList<AdminMemberListItemDto>> Handle(GetAdminMembersQuery request, CancellationToken cancellationToken)
    {
        return await _adminMemberQueries.GetAllAsync(cancellationToken);
    }
}
