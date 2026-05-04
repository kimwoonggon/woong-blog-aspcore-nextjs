using WoongBlog.Infrastructure.LoadTesting;

namespace WoongBlog.Api.Modules.Diagnostics;

internal static class RealLoadTestsEndpoint
{
    private const string BasePath = "/api/admin/load-tests/real";

    internal static void MapRealBackendLoadTests(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                $"{BasePath}/start",
                async (StartRealLoadTestRequest request, IRealLoadTestControlPlane controlPlane, CancellationToken cancellationToken) =>
                    await StartAsync(request, controlPlane, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("StartRealBackendLoadTest")
            .Produces<RealLoadTestStartResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        app.MapGet(
                $"{BasePath}/{{runId}}",
                async (string runId, IRealLoadTestControlPlane controlPlane, CancellationToken cancellationToken) =>
                    await GetStatusAsync(runId, controlPlane, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("GetRealBackendLoadTestStatus")
            .Produces<RealLoadTestStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet(
                $"{BasePath}/{{runId}}/metrics",
                async (string runId, IRealLoadTestControlPlane controlPlane, CancellationToken cancellationToken) =>
                    await GetMetricsAsync(runId, controlPlane, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("GetRealBackendLoadTestMetrics")
            .Produces<RealLoadTestMetricsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost(
                $"{BasePath}/{{runId}}/stop",
                async (string runId, IRealLoadTestControlPlane controlPlane, CancellationToken cancellationToken) =>
                    await StopAsync(runId, controlPlane, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("StopRealBackendLoadTest")
            .Produces<RealLoadTestStopResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> StartAsync(
        StartRealLoadTestRequest request,
        IRealLoadTestControlPlane controlPlane,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await controlPlane.StartAsync(
                new RealLoadTestStartRequest(
                    request.Scenario,
                    request.Runner,
                    request.Target,
                    request.Rate,
                    request.DurationSeconds,
                    request.MaxVus),
                cancellationToken);
            return Results.Ok(response);
        }
        catch (RealLoadTestValidationException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (RealLoadTestConflictException exception)
        {
            return Results.Conflict(new { error = exception.Message });
        }
    }

    private static async Task<IResult> GetStatusAsync(
        string runId,
        IRealLoadTestControlPlane controlPlane,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await controlPlane.GetStatusAsync(runId, cancellationToken));
        }
        catch (RealLoadTestNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMetricsAsync(
        string runId,
        IRealLoadTestControlPlane controlPlane,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await controlPlane.GetMetricsAsync(runId, cancellationToken));
        }
        catch (RealLoadTestNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> StopAsync(
        string runId,
        IRealLoadTestControlPlane controlPlane,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await controlPlane.StopAsync(runId, cancellationToken));
        }
        catch (RealLoadTestNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private sealed record StartRealLoadTestRequest(
        string Scenario,
        string Runner,
        string Target,
        int Rate,
        int DurationSeconds,
        int MaxVus);
}
