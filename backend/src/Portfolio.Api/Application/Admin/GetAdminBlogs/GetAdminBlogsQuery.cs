using MediatR;

namespace Portfolio.Api.Application.Admin.GetAdminBlogs;

public sealed record GetAdminBlogsQuery : IRequest<IReadOnlyList<AdminBlogListItemDto>>;
