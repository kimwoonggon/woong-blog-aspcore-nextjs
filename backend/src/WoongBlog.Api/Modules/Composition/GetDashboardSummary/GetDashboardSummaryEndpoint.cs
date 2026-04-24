using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Composition.GetDashboardSummary;

namespace WoongBlog.Api.Modules.Composition.GetDashboardSummary;

internal static class GetDashboardSummaryEndpoint
{
    internal static void MapGetDashboardSummary(this IEndpointRouteBuilder app)
    {
        app.MapGet(CompositionApiPaths.GetDashboard, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Dashboard")
            .WithName("GetDashboardSummary")
            .Produces<AdminDashboardSummaryDto>(StatusCodes.Status200OK);
    }
}
