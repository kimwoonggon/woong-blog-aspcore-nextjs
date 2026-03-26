using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.CreateBlog;

public sealed record CreateBlogCommand(
    string Title,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult>;
