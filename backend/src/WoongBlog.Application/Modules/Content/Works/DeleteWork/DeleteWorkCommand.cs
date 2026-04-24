using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Works.DeleteWork;

public sealed record DeleteWorkCommand(Guid Id) : IRequest<AdminActionResult>;
