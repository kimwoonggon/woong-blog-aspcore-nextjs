using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Works.Application.DeleteWork;

public sealed record DeleteWorkCommand(Guid Id) : IRequest<AdminActionResult>;
