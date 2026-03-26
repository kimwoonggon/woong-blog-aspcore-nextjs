using MediatR;

namespace Portfolio.Api.Application.Admin.GetAdminBlogById;

public sealed record GetAdminBlogByIdQuery(Guid Id) : IRequest<AdminBlogDetailDto?>;
