using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.UpdateBlog;

public sealed record UpdateBlogCommand(
    Guid Id,
    string Title,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult?>;
