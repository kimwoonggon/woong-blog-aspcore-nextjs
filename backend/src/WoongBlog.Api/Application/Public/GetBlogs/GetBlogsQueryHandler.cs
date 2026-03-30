using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetBlogs;

public class GetBlogsQueryHandler : IRequestHandler<GetBlogsQuery, PagedBlogsDto>
{
    private readonly IPublicBlogQueries _publicBlogQueries;

    public GetBlogsQueryHandler(IPublicBlogQueries publicBlogQueries)
    {
        _publicBlogQueries = publicBlogQueries;
    }

    public async Task<PagedBlogsDto> Handle(GetBlogsQuery request, CancellationToken cancellationToken)
    {
        return await _publicBlogQueries.GetBlogsAsync(request.Page, request.PageSize, cancellationToken);
    }
}
