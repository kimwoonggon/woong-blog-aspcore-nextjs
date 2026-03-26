using MediatR;

namespace Portfolio.Api.Application.Admin.GetAdminWorkById;

public sealed record GetAdminWorkByIdQuery(Guid Id) : IRequest<AdminWorkDetailDto?>;
