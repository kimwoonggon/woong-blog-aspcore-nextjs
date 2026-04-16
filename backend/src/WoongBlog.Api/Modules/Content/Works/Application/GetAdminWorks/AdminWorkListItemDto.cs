namespace WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;

public sealed record AdminWorkListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string Category,
    string? Period,
    string[] Tags,
    string? ThumbnailUrl,
    bool Published,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
