namespace WoongBlog.Api.Application.Admin.GetAdminWorks;

public sealed record AdminWorkListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string Category,
    string? Period,
    string[] Tags,
    bool Published,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
