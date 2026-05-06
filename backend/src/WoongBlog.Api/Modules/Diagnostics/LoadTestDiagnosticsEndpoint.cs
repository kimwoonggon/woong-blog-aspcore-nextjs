using WoongBlog.Infrastructure.LoadTesting;

namespace WoongBlog.Api.Modules.Diagnostics;

internal static class LoadTestDiagnosticsEndpoint
{
    private const string Path = "/api/admin/load-test/diagnostics";

    internal static void MapLoadTestDiagnostics(this IEndpointRouteBuilder app)
    {
        app.MapRealBackendLoadTests();

        app.MapGet(
                Path,
                async (ILoadTestDiagnosticsSampler diagnosticsSampler, CancellationToken cancellationToken) =>
                    Results.Ok(await diagnosticsSampler.CaptureAsync(cancellationToken)))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("GetLoadTestDiagnostics")
            .Produces<LoadTestDiagnosticsSnapshot>(StatusCodes.Status200OK);
    }
}
