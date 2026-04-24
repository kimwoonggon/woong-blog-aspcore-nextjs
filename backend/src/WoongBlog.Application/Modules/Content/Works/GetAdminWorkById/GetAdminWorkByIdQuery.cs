using MediatR;

namespace WoongBlog.Application.Modules.Content.Works.GetAdminWorkById;

public sealed record GetAdminWorkByIdQuery(Guid Id) : IRequest<AdminWorkDetailDto?>;
