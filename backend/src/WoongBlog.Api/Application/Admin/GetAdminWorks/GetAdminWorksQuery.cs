using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminWorks;

public sealed record GetAdminWorksQuery : IRequest<IReadOnlyList<AdminWorkListItemDto>>;
