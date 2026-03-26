using MediatR;
using Portfolio.Api.Application.Admin.Abstractions;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.DeleteBlog;

public sealed class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, AdminActionResult>
{
    private readonly IAdminBlogService _adminBlogService;

    public DeleteBlogCommandHandler(IAdminBlogService adminBlogService)
    {
        _adminBlogService = adminBlogService;
    }

    public async Task<AdminActionResult> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
    {
        return await _adminBlogService.DeleteAsync(request.Id, cancellationToken);
    }
}
