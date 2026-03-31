using MediatR;

namespace WoongBlog.Api.Modules.Composition.Application.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<AdminDashboardSummaryDto>;
