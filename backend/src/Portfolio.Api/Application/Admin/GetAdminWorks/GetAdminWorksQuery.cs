using MediatR;

namespace Portfolio.Api.Application.Admin.GetAdminWorks;

public sealed record GetAdminWorksQuery : IRequest<IReadOnlyList<AdminWorkListItemDto>>;
