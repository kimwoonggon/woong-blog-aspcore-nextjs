using MediatR;
using Portfolio.Api.Application.Admin.Abstractions;

namespace Portfolio.Api.Application.Admin.GetAdminBlogById;

public sealed class GetAdminBlogByIdQueryHandler : IRequestHandler<GetAdminBlogByIdQuery, AdminBlogDetailDto?>
{
    private readonly IAdminBlogService _adminBlogService;

    public GetAdminBlogByIdQueryHandler(IAdminBlogService adminBlogService)
    {
        _adminBlogService = adminBlogService;
    }

    public async Task<AdminBlogDetailDto?> Handle(GetAdminBlogByIdQuery request, CancellationToken cancellationToken)
    {
        return await _adminBlogService.GetByIdAsync(request.Id, cancellationToken);
    }
}
