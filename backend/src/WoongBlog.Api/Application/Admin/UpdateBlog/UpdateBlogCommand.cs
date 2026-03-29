using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdateBlog;

public sealed record UpdateBlogCommand(
    Guid Id,
    string Title,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult?>;
