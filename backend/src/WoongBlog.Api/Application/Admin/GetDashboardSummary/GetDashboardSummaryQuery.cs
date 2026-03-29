using MediatR;

namespace WoongBlog.Api.Application.Admin.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<AdminDashboardSummaryDto>;
