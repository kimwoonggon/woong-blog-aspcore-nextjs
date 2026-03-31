using MediatR;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;

namespace WoongBlog.Api.Modules.Composition.Application.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, AdminDashboardSummaryDto>
{
    private readonly IAdminDashboardService _adminDashboardService;

    public GetDashboardSummaryQueryHandler(IAdminDashboardService adminDashboardService)
    {
        _adminDashboardService = adminDashboardService;
    }

    public async Task<AdminDashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _adminDashboardService.GetSummaryAsync(cancellationToken);
    }
}
