using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Blogs.DeleteBlog;

public sealed record DeleteBlogCommand(Guid Id) : IRequest<AdminActionResult>;
