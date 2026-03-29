namespace WoongBlog.Api.Application.Admin.GetDashboardSummary;

public sealed record AdminDashboardSummaryDto(
    int WorksCount,
    int BlogsCount,
    int ViewsCount
);
