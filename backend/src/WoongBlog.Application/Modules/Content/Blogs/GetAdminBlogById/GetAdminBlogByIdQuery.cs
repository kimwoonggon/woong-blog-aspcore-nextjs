using MediatR;

namespace WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogById;

public sealed record GetAdminBlogByIdQuery(Guid Id) : IRequest<AdminBlogDetailDto?>;
