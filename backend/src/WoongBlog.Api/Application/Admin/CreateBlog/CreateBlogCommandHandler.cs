using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.CreateBlog;

public sealed class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, AdminMutationResult>
{
    private readonly IAdminBlogService _adminBlogService;

    public CreateBlogCommandHandler(IAdminBlogService adminBlogService)
    {
        _adminBlogService = adminBlogService;
    }

    public async Task<AdminMutationResult> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        return await _adminBlogService.CreateAsync(request, cancellationToken);
    }
}
