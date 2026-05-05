using WoongBlog.Application.Modules.Content.Common;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;

public sealed record BlogDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    PublicContentBodyDto Content,
    string[] Tags,
    string CoverUrl,
    DateTimeOffset? PublishedAt
);
