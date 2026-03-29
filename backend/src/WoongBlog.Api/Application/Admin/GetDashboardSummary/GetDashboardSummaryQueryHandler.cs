using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetDashboardSummary;

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
