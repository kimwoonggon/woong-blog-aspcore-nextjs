using MediatR;

namespace Portfolio.Api.Application.Public.GetPageBySlug;

public sealed record GetPageBySlugQuery(string Slug) : IRequest<PageDto?>;
