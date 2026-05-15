using MediatR;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkDetailContext;

public sealed record GetWorkDetailContextQuery(string Slug, int Limit = 9) : IRequest<WorkDetailContextDto?>;
