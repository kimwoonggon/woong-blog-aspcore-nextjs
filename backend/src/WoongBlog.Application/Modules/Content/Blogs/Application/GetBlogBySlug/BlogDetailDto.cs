namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;

public sealed record BlogDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string ContentJson,
    string[] Tags,
    string CoverUrl,
    DateTimeOffset? PublishedAt
);
