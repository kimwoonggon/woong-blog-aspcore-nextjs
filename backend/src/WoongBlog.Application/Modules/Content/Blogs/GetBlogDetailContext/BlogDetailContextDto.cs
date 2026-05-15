using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogDetailContext;

public sealed record BlogDetailContextDto(
    BlogCardDto? Newer,
    BlogCardDto? Older,
    IReadOnlyList<BlogCardDto> Related);
