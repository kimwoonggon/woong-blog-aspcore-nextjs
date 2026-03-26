using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.DeleteBlog;

public sealed record DeleteBlogCommand(Guid Id) : IRequest<AdminActionResult>;
