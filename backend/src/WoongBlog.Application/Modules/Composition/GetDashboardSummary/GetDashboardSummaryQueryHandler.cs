using MediatR;
using WoongBlog.Application.Modules.Composition.Abstractions;

namespace WoongBlog.Application.Modules.Composition.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, AdminDashboardSummaryDto>
{
    private readonly IAdminDashboardQueryStore _adminDashboardQueryStore;

    public GetDashboardSummaryQueryHandler(IAdminDashboardQueryStore adminDashboardQueryStore)
    {
        _adminDashboardQueryStore = adminDashboardQueryStore;
    }

    public async Task<AdminDashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _adminDashboardQueryStore.GetSummaryAsync(cancellationToken);
    }
}
