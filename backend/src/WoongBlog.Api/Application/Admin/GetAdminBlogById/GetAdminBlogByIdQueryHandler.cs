using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminBlogById;

public sealed class GetAdminBlogByIdQueryHandler : IRequestHandler<GetAdminBlogByIdQuery, AdminBlogDetailDto?>
{
    private readonly IAdminBlogQueries _adminBlogQueries;

    public GetAdminBlogByIdQueryHandler(IAdminBlogQueries adminBlogQueries)
    {
        _adminBlogQueries = adminBlogQueries;
    }

    public async Task<AdminBlogDetailDto?> Handle(GetAdminBlogByIdQuery request, CancellationToken cancellationToken)
    {
        return await _adminBlogQueries.GetByIdAsync(request.Id, cancellationToken);
    }
}
