using MediatR;

namespace WoongBlog.Api.Application.Public.GetPageBySlug;

public sealed record GetPageBySlugQuery(string Slug) : IRequest<PageDto?>;
