using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Blogs.CreateBlog;

public sealed record CreateBlogCommand(
    string Title,
    string? Excerpt,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult>;
