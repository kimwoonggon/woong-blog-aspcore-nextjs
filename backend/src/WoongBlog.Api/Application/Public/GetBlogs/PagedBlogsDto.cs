using WoongBlog.Api.Application.Public.GetHome;

namespace WoongBlog.Api.Application.Public.GetBlogs;

public sealed record PagedBlogsDto(
    IReadOnlyList<BlogCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
