using MediatR;

namespace Portfolio.Api.Application.Admin.GetAdminMembers;

public sealed record GetAdminMembersQuery : IRequest<IReadOnlyList<AdminMemberListItemDto>>;
