using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;

namespace WoongBlog.Application.Modules.Content.Pages.UpdatePage;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, AdminActionResult>
{
    private readonly IPageCommandStore _pageCommandStore;

    public UpdatePageCommandHandler(IPageCommandStore pageCommandStore)
    {
        _pageCommandStore = pageCommandStore;
    }

    public async Task<AdminActionResult> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageCommandStore.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (page is null)
        {
            return new AdminActionResult(false);
        }

        page.Title = request.Title;
        page.ContentJson = request.ContentJson;
        page.UpdatedAt = DateTimeOffset.UtcNow;

        await _pageCommandStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
