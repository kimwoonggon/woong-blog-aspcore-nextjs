namespace WoongBlog.Application.Modules.Content.Works.GetWorks;

public sealed record WorkCardDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string Category,
    string[] Tags,
    string? ThumbnailUrl,
    DateTimeOffset? PublishedAt
);

public sealed record PagedWorksDto(
    IReadOnlyList<WorkCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
