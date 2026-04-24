namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogs;

public sealed record BlogCardDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string[] Tags,
    string? CoverUrl,
    DateTimeOffset? PublishedAt
);

public sealed record PagedBlogsDto(
    IReadOnlyList<BlogCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
