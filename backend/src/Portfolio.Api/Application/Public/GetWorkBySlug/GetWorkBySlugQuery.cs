using MediatR;

namespace Portfolio.Api.Application.Public.GetWorkBySlug;

public sealed record GetWorkBySlugQuery(string Slug) : IRequest<WorkDetailDto?>;
