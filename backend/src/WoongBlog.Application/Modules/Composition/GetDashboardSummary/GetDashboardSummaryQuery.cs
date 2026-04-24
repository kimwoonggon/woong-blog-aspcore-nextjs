using MediatR;

namespace WoongBlog.Application.Modules.Composition.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<AdminDashboardSummaryDto>;
