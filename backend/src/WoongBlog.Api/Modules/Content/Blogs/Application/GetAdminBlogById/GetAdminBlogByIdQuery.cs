using MediatR;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogById;

public sealed record GetAdminBlogByIdQuery(Guid Id) : IRequest<AdminBlogDetailDto?>;
