using MediatR;

namespace WoongBlog.Application.Modules.Content.Works.GetAdminWorks;

public sealed record GetAdminWorksQuery : IRequest<IReadOnlyList<AdminWorkListItemDto>>;
