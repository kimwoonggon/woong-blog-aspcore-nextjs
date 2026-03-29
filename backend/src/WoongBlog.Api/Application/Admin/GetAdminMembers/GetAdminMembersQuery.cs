using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminMembers;

public sealed record GetAdminMembersQuery : IRequest<IReadOnlyList<AdminMemberListItemDto>>;
