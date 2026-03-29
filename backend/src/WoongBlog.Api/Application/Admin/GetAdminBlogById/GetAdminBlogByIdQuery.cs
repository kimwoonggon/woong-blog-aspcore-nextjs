using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminBlogById;

public sealed record GetAdminBlogByIdQuery(Guid Id) : IRequest<AdminBlogDetailDto?>;
