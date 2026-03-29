using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdatePage;

public sealed record UpdatePageCommand(
    Guid Id,
    string Title,
    string ContentJson
) : IRequest<AdminActionResult>;
