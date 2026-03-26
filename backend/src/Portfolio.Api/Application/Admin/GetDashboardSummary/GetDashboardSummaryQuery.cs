using MediatR;

namespace Portfolio.Api.Application.Admin.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<AdminDashboardSummaryDto>;
