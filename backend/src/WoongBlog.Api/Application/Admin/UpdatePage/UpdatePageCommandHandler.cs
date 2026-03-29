using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdatePage;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, AdminActionResult>
{
    private readonly IAdminPageService _adminPageService;

    public UpdatePageCommandHandler(IAdminPageService adminPageService)
    {
        _adminPageService = adminPageService;
    }

    public async Task<AdminActionResult> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        return await _adminPageService.UpdatePageAsync(request.Id, request.Title, request.ContentJson, cancellationToken);
    }
}
