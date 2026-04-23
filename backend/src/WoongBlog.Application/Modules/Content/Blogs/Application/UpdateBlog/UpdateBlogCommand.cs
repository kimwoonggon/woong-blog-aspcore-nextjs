using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.UpdateBlog;

public sealed record UpdateBlogCommand(
    Guid Id,
    string Title,
    string? Excerpt,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult?>;
