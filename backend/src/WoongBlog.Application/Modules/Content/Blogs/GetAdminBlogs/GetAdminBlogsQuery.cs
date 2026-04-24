using MediatR;

namespace WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogs;

public sealed record GetAdminBlogsQuery : IRequest<IReadOnlyList<AdminBlogListItemDto>>;
