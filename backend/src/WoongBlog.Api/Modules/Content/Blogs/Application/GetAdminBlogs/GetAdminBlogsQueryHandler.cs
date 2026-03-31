using MediatR;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogs;

public sealed class GetAdminBlogsQueryHandler : IRequestHandler<GetAdminBlogsQuery, IReadOnlyList<AdminBlogListItemDto>>
{
    private readonly IAdminBlogService _adminBlogService;

    public GetAdminBlogsQueryHandler(IAdminBlogService adminBlogService)
    {
        _adminBlogService = adminBlogService;
    }

    public async Task<IReadOnlyList<AdminBlogListItemDto>> Handle(GetAdminBlogsQuery request, CancellationToken cancellationToken)
    {
        return await _adminBlogService.GetAllAsync(cancellationToken);
    }
}
