using MediatR;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;

public class GetBlogsQueryHandler : IRequestHandler<GetBlogsQuery, PagedBlogsDto>
{
    private readonly IPublicBlogService _publicBlogService;

    public GetBlogsQueryHandler(IPublicBlogService publicBlogService)
    {
        _publicBlogService = publicBlogService;
    }

    public async Task<PagedBlogsDto> Handle(GetBlogsQuery request, CancellationToken cancellationToken)
    {
        return await _publicBlogService.GetBlogsAsync(request, cancellationToken);
    }
}
