using WoongBlog.Application.Modules.Content.Pages.UpdatePage;

namespace WoongBlog.Api.Modules.Content.Pages.UpdatePage;

public sealed class UpdatePageRequest
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ContentJson { get; init; } = string.Empty;

    internal UpdatePageCommand ToCommand() => new(Id, Title, ContentJson);
}
