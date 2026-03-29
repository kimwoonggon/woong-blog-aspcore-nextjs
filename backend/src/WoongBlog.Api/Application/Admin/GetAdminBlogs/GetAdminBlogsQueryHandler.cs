using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminBlogs;

public sealed class GetAdminBlogsQueryHandler : IRequestHandler<GetAdminBlogsQuery, IReadOnlyList<AdminBlogListItemDto>>
{
    private readonly IAdminBlogQueries _adminBlogQueries;

    public GetAdminBlogsQueryHandler(IAdminBlogQueries adminBlogQueries)
    {
        _adminBlogQueries = adminBlogQueries;
    }

    public async Task<IReadOnlyList<AdminBlogListItemDto>> Handle(GetAdminBlogsQuery request, CancellationToken cancellationToken)
    {
        return await _adminBlogQueries.GetAllAsync(cancellationToken);
    }
}
