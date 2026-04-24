using MediatR;

namespace WoongBlog.Application.Modules.Content.Works.GetWorks;

public sealed record GetWorksQuery(
    int Page = 1,
    int PageSize = 6,
    string? Query = null,
    string? SearchMode = null) : IRequest<PagedWorksDto>;
