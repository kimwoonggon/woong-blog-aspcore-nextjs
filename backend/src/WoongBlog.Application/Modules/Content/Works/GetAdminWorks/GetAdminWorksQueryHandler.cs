using MediatR;
using WoongBlog.Application.Modules.Content.Works.Abstractions;

namespace WoongBlog.Application.Modules.Content.Works.GetAdminWorks;

public sealed class GetAdminWorksQueryHandler : IRequestHandler<GetAdminWorksQuery, IReadOnlyList<AdminWorkListItemDto>>
{
    private readonly IWorkQueryStore _workQueryStore;

    public GetAdminWorksQueryHandler(IWorkQueryStore workQueryStore)
    {
        _workQueryStore = workQueryStore;
    }

    public async Task<IReadOnlyList<AdminWorkListItemDto>> Handle(GetAdminWorksQuery request, CancellationToken cancellationToken)
    {
        return await _workQueryStore.GetAdminListAsync(cancellationToken);
    }
}
