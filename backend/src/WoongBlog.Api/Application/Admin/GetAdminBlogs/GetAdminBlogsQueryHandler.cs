using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminBlogs;

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
