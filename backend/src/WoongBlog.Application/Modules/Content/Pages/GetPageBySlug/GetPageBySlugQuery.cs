using MediatR;

namespace WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

public sealed record GetPageBySlugQuery(string Slug) : IRequest<PageDto?>;
