using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminWorkById;

public sealed record GetAdminWorkByIdQuery(Guid Id) : IRequest<AdminWorkDetailDto?>;
