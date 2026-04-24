using MediatR;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;

public sealed record GetWorkBySlugQuery(string Slug) : IRequest<WorkDetailDto?>;
