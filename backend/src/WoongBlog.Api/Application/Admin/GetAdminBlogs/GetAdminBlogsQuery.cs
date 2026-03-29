using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminBlogs;

public sealed record GetAdminBlogsQuery : IRequest<IReadOnlyList<AdminBlogListItemDto>>;
