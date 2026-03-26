using MediatR;

namespace Portfolio.Api.Application.Public.GetBlogs;

public sealed record GetBlogsQuery(int Page = 1, int PageSize = 10) : IRequest<PagedBlogsDto>;
