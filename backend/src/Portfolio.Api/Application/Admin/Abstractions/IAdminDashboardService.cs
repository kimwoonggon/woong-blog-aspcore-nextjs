using Portfolio.Api.Application.Admin.GetDashboardSummary;

namespace Portfolio.Api.Application.Admin.Abstractions;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
