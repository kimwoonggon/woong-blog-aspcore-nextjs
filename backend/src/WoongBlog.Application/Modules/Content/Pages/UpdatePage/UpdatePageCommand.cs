using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Pages.UpdatePage;

public sealed record UpdatePageCommand(
    Guid Id,
    string Title,
    string ContentJson
) : IRequest<AdminActionResult>;
