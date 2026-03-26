using Portfolio.Api.Application.Public.GetHome;

namespace Portfolio.Api.Application.Public.GetBlogs;

public sealed record PagedBlogsDto(
    IReadOnlyList<BlogCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
