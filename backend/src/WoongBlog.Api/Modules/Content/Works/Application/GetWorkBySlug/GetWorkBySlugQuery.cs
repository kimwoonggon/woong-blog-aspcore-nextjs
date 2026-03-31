using MediatR;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;

public sealed record GetWorkBySlugQuery(string Slug) : IRequest<WorkDetailDto?>;
