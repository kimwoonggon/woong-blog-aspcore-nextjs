using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.DeleteWork;

public sealed record DeleteWorkCommand(Guid Id) : IRequest<AdminActionResult>;
