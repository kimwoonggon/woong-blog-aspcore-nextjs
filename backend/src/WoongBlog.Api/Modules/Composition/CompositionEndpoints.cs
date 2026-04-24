using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Composition.GetDashboardSummary;
using WoongBlog.Api.Modules.Composition.GetHome;

namespace WoongBlog.Api.Modules.Composition;

internal static class CompositionEndpoints
{
    internal static void MapCompositionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetHome();
        app.MapGetDashboardSummary();
    }
}
