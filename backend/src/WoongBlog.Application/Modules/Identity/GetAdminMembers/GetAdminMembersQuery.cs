using MediatR;

namespace WoongBlog.Application.Modules.Identity.GetAdminMembers;

public sealed record GetAdminMembersQuery : IRequest<IReadOnlyList<AdminMemberListItemDto>>;
