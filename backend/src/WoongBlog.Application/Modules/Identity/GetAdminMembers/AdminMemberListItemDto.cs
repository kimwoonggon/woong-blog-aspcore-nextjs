namespace WoongBlog.Application.Modules.Identity.GetAdminMembers;

public sealed record AdminMemberListItemDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    string Provider,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    int ActiveSessionCount
);
