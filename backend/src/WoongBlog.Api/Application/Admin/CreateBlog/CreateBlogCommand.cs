using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.CreateBlog;

public sealed record CreateBlogCommand(
    string Title,
    string[] Tags,
    bool Published,
    string ContentJson
) : IRequest<AdminMutationResult>;
