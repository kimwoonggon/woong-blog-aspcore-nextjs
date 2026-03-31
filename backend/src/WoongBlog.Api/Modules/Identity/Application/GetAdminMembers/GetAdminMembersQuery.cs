using MediatR;

namespace WoongBlog.Api.Modules.Identity.Application.GetAdminMembers;

public sealed record GetAdminMembersQuery : IRequest<IReadOnlyList<AdminMemberListItemDto>>;
