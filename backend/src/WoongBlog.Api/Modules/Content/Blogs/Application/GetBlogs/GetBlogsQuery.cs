using MediatR;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;

public sealed record GetBlogsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Query = null,
    string SearchMode = "title") : IRequest<PagedBlogsDto>;
