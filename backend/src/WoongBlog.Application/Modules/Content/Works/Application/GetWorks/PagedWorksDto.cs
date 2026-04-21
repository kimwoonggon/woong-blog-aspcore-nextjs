namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

public sealed record WorkCardDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string Category,
    string? Period,
    string[] Tags,
    string? ThumbnailUrl,
    string? IconUrl,
    DateTimeOffset? PublishedAt
);

public sealed record PagedWorksDto(
    IReadOnlyList<WorkCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
