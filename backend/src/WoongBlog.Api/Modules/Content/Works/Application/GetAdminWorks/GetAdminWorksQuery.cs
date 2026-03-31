using MediatR;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;

public sealed record GetAdminWorksQuery : IRequest<IReadOnlyList<AdminWorkListItemDto>>;
