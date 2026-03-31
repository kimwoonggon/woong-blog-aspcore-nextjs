using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.CreateBlog;

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
