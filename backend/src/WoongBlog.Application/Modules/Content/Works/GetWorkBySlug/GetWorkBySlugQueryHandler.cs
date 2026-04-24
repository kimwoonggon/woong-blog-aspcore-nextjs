using MediatR;
using WoongBlog.Application.Modules.Content.Works.Abstractions;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;

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
