using MediatR;

namespace WoongBlog.Application.Modules.Content.Pages.GetAdminPages;

public sealed record GetAdminPagesQuery(string[]? Slugs) : IRequest<IReadOnlyList<AdminPageListItemDto>>;
