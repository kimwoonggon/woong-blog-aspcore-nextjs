using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.DeleteBlog;

public sealed record DeleteBlogCommand(Guid Id) : IRequest<AdminActionResult>;
