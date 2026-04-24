using WoongBlog.Application.Modules.Composition.GetDashboardSummary;

namespace WoongBlog.Application.Modules.Composition.Abstractions;

public interface IAdminDashboardQueryStore
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
