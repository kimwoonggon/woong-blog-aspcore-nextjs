using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.DeleteWork;

public sealed record DeleteWorkCommand(Guid Id) : IRequest<AdminActionResult>;
