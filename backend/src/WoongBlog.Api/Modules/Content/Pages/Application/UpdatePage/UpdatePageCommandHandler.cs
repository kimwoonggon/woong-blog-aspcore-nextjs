using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Pages.Application.UpdatePage;

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
