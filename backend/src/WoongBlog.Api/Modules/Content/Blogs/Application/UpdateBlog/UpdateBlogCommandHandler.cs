using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.UpdateBlog;

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
