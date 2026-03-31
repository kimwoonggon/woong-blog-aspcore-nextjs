using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.DeleteBlog;

public sealed record DeleteBlogCommand(Guid Id) : IRequest<AdminActionResult>;
