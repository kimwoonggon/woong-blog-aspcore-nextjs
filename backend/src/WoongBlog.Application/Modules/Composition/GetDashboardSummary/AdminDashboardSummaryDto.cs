namespace WoongBlog.Application.Modules.Composition.GetDashboardSummary;

public sealed record AdminDashboardSummaryDto(
    int WorksCount,
    int BlogsCount,
    int ViewsCount
);
