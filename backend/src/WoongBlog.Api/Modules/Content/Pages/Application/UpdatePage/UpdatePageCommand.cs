using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Pages.Application.UpdatePage;

public sealed record UpdatePageCommand(
    Guid Id,
    string Title,
    string ContentJson
) : IRequest<AdminActionResult>;
