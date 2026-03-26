using MediatR;

namespace Portfolio.Api.Application.Public.GetBlogBySlug;

public sealed record GetBlogBySlugQuery(string Slug) : IRequest<BlogDetailDto?>;
