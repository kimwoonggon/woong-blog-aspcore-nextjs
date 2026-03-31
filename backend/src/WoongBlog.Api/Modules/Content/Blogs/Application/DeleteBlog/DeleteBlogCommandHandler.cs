using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.DeleteBlog;

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
