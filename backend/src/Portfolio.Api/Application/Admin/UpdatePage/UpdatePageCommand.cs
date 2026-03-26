using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.UpdatePage;

public sealed record UpdatePageCommand(
    Guid Id,
    string Title,
    string ContentJson
) : IRequest<AdminActionResult>;
