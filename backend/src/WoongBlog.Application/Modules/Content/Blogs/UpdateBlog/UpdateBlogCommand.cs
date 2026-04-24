using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Blogs.UpdateBlog;

public sealed record UpdateBlogCommand(
    Guid Id,
    string Title,
    string? Excerpt,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult?>;
