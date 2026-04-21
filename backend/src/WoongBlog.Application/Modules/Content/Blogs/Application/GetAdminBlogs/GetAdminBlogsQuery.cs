using MediatR;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogs;

public sealed record GetAdminBlogsQuery : IRequest<IReadOnlyList<AdminBlogListItemDto>>;
