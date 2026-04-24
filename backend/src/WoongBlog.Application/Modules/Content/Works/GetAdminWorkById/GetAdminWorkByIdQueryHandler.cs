using MediatR;
using WoongBlog.Application.Modules.Content.Works.Abstractions;

namespace WoongBlog.Application.Modules.Content.Works.GetAdminWorkById;

public sealed class GetAdminWorkByIdQueryHandler : IRequestHandler<GetAdminWorkByIdQuery, AdminWorkDetailDto?>
{
    private readonly IWorkQueryStore _workQueryStore;

    public GetAdminWorkByIdQueryHandler(IWorkQueryStore workQueryStore)
    {
        _workQueryStore = workQueryStore;
    }

    public async Task<AdminWorkDetailDto?> Handle(GetAdminWorkByIdQuery request, CancellationToken cancellationToken)
    {
        return await _workQueryStore.GetAdminDetailAsync(request.Id, cancellationToken);
    }
}
