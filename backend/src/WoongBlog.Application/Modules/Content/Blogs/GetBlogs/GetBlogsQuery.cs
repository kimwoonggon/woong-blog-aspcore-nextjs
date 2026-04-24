using MediatR;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogs;

public sealed record GetBlogsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Query = null,
    string? SearchMode = null) : IRequest<PagedBlogsDto>;
