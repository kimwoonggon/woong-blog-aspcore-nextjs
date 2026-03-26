using MediatR;
using Portfolio.Api.Application.Admin.Abstractions;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.UpdateBlog;

public sealed class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, AdminMutationResult?>
{
    private readonly IAdminBlogService _adminBlogService;

    public UpdateBlogCommandHandler(IAdminBlogService adminBlogService)
    {
        _adminBlogService = adminBlogService;
    }

    public async Task<AdminMutationResult?> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
    {
        return await _adminBlogService.UpdateAsync(request, cancellationToken);
    }
}
