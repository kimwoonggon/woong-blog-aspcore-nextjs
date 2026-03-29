using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminPages;

public sealed record GetAdminPagesQuery(string[]? Slugs) : IRequest<IReadOnlyList<AdminPageListItemDto>>;
