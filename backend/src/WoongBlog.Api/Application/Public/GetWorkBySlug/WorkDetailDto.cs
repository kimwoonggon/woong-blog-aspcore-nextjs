namespace WoongBlog.Api.Application.Public.GetWorkBySlug;

public sealed record WorkDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string ContentJson,
    string Category,
    string? Period,
    string[] Tags,
    string ThumbnailUrl,
    string IconUrl,
    DateTimeOffset? PublishedAt
);
