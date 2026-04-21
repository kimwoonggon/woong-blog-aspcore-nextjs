using MediatR;

namespace WoongBlog.Api.Modules.Content.Pages.Application.GetAdminPages;

public sealed record GetAdminPagesQuery(string[]? Slugs) : IRequest<IReadOnlyList<AdminPageListItemDto>>;
