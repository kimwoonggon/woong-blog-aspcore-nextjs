using MediatR;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;

public class GetWorkBySlugQueryHandler : IRequestHandler<GetWorkBySlugQuery, WorkDetailDto?>
{
    private readonly IWorkQueryStore _workQueryStore;

    public GetWorkBySlugQueryHandler(IWorkQueryStore workQueryStore)
    {
        _workQueryStore = workQueryStore;
    }

    public async Task<WorkDetailDto?> Handle(GetWorkBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _workQueryStore.GetPublishedDetailBySlugAsync(request.Slug, cancellationToken);
    }
}
