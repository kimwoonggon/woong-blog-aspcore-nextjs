using WoongBlog.Api.Modules.Composition.Application.GetDashboardSummary;

namespace WoongBlog.Api.Modules.Composition.Application.Abstractions;

public interface IAdminDashboardQueryStore
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
