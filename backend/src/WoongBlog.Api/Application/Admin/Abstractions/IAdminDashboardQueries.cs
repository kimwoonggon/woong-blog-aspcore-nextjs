using WoongBlog.Api.Application.Admin.GetDashboardSummary;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminDashboardQueries
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
