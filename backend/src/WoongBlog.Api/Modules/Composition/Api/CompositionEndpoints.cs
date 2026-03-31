using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Composition.Api.GetDashboardSummary;
using WoongBlog.Api.Modules.Composition.Api.GetHome;

namespace WoongBlog.Api.Modules.Composition.Api;

internal static class CompositionEndpoints
{
    internal static void MapCompositionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetHome();
        app.MapGetDashboardSummary();
    }
}
