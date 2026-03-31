namespace WoongBlog.Api.Modules.Composition.Application.GetDashboardSummary;

public sealed record AdminDashboardSummaryDto(
    int WorksCount,
    int BlogsCount,
    int ViewsCount
);
