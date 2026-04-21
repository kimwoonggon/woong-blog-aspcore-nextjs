using MediatR;

namespace WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

public sealed record GetPageBySlugQuery(string Slug) : IRequest<PageDto?>;
