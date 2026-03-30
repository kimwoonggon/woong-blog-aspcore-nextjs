using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdatePage;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, AdminActionResult>
{
    private readonly IAdminPageWriteStore _adminPageWriteStore;

    public UpdatePageCommandHandler(IAdminPageWriteStore adminPageWriteStore)
    {
        _adminPageWriteStore = adminPageWriteStore;
    }

    public async Task<AdminActionResult> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _adminPageWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (page is null)
        {
            return new AdminActionResult(false);
        }

        page.UpdateContent(request.Title, request.ContentJson, DateTimeOffset.UtcNow);

        await _adminPageWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
