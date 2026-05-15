using MediatR;
using WoongBlog.Application.Modules.Content.Works.Abstractions;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkDetailContext;

public sealed class GetWorkDetailContextQueryHandler(IWorkQueryStore workQueryStore)
    : IRequestHandler<GetWorkDetailContextQuery, WorkDetailContextDto?>
{
    public async Task<WorkDetailContextDto?> Handle(
        GetWorkDetailContextQuery request,
        CancellationToken cancellationToken)
    {
        return await workQueryStore.GetPublishedDetailContextBySlugAsync(
            request.Slug,
            request.Limit,
            cancellationToken);
    }
}
