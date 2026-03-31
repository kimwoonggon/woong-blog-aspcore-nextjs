using WoongBlog.Api.Modules.Composition.Application.GetHome;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;

public sealed record PagedBlogsDto(
    IReadOnlyList<BlogCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
