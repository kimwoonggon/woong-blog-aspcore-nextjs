using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.CreateBlog;

public sealed record CreateBlogCommand(
    string Title,
    string? Excerpt,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult>;
