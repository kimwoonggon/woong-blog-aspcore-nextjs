using MediatR;

namespace WoongBlog.Api.Application.Public.GetWorkBySlug;

public sealed record GetWorkBySlugQuery(string Slug) : IRequest<WorkDetailDto?>;
