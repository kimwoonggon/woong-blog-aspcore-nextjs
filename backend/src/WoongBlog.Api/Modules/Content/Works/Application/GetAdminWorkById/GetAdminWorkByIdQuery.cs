using MediatR;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorkById;

public sealed record GetAdminWorkByIdQuery(Guid Id) : IRequest<AdminWorkDetailDto?>;
