namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogs;

public sealed record AdminBlogListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string[] Tags,
    bool Published,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
