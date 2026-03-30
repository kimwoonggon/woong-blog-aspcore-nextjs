using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, AdminDashboardSummaryDto>
{
    private readonly IAdminDashboardQueries _adminDashboardQueries;

    public GetDashboardSummaryQueryHandler(IAdminDashboardQueries adminDashboardQueries)
    {
        _adminDashboardQueries = adminDashboardQueries;
    }

    public async Task<AdminDashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _adminDashboardQueries.GetSummaryAsync(cancellationToken);
    }
}
