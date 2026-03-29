namespace WoongBlog.Api.Application.Admin.GetAdminBlogById;

public sealed record AdminBlogContentDto(string Html);

public sealed record AdminBlogDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string[] Tags,
    bool Published,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt,
    AdminBlogContentDto Content
);
