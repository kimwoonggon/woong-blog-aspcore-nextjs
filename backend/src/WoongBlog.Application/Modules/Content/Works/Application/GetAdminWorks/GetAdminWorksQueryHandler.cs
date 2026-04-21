using MediatR;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;

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
