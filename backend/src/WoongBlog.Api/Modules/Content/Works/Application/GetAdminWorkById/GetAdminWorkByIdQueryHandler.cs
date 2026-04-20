using MediatR;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorkById;

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
