using WoongBlog.Infrastructure.LoadTesting;

namespace WoongBlog.Api.Modules.Diagnostics;

internal static class RealLoadTestsEndpoint
{
    private const string BasePath = "/api/admin/load-tests/real";

    internal static void MapRealBackendLoadTests(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                $"{BasePath}/start",
                async (
                    StartRealLoadTestRequest request,
                    IRealLoadTestControlPlane controlPlane,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    await StartAsync(request, controlPlane, loggerFactory, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("StartRealBackendLoadTest")
            .Produces<RealLoadTestStartResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        app.MapGet(
                $"{BasePath}/{{runId}}",
                async (string runId, IRealLoadTestControlPlane controlPlane, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
                    await GetStatusAsync(runId, controlPlane, loggerFactory, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("GetRealBackendLoadTestStatus")
            .Produces<RealLoadTestStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet(
                $"{BasePath}/{{runId}}/metrics",
                async (string runId, IRealLoadTestControlPlane controlPlane, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
                    await GetMetricsAsync(runId, controlPlane, loggerFactory, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("GetRealBackendLoadTestMetrics")
            .Produces<RealLoadTestMetricsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost(
                $"{BasePath}/{{runId}}/stop",
                async (string runId, IRealLoadTestControlPlane controlPlane, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
                    await StopAsync(runId, controlPlane, loggerFactory, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Diagnostics")
            .WithName("StopRealBackendLoadTest")
            .Produces<RealLoadTestStopResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> StartAsync(
        StartRealLoadTestRequest request,
        IRealLoadTestControlPlane controlPlane,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("RealLoadTestsEndpoint");
        try
        {
            var response = await controlPlane.StartAsync(
                new RealLoadTestStartRequest(
                    request.Scenario,
                    request.Runner,
                    request.Target,
                    request.Rate,
                    request.PeakRate,
                    request.DurationSeconds,
                    request.MaxVus,
                    request.StartVus,
                    request.Targets ?? Array.Empty<RealLoadTestTargetSpec>()),
                cancellationToken);
            return Results.Ok(response);
        }
        catch (RealLoadTestValidationException exception)
        {
            logger.LogWarning(
                exception,
                "Real load test start failed validation. scenario={Scenario}, runner={Runner}, target={Target}, rate={Rate}, durationSeconds={DurationSeconds}, maxVUS={MaxVUS}",
                request.Scenario,
                request.Runner,
                request.Target,
                request.Rate,
                request.DurationSeconds,
                request.MaxVus);
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (RealLoadTestConflictException exception)
        {
            logger.LogWarning(
                exception,
                "Real load test start blocked by conflict. scenario={Scenario}, runner={Runner}, target={Target}",
                request.Scenario,
                request.Runner,
                request.Target);
            return Results.Conflict(new { error = exception.Message });
        }
    }

    private static async Task<IResult> GetStatusAsync(
        string runId,
        IRealLoadTestControlPlane controlPlane,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("RealLoadTestsEndpoint");
        try
        {
            return Results.Ok(await controlPlane.GetStatusAsync(runId, cancellationToken));
        }
        catch (RealLoadTestNotFoundException)
        {
            logger.LogWarning("Real load test status lookup not found for runId={RunId}", runId);
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMetricsAsync(
        string runId,
        IRealLoadTestControlPlane controlPlane,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("RealLoadTestsEndpoint");
        try
        {
            return Results.Ok(await controlPlane.GetMetricsAsync(runId, cancellationToken));
        }
        catch (RealLoadTestNotFoundException)
        {
            logger.LogWarning("Real load test metrics lookup not found for runId={RunId}", runId);
            return Results.NotFound();
        }
    }

    private static async Task<IResult> StopAsync(
        string runId,
        IRealLoadTestControlPlane controlPlane,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("RealLoadTestsEndpoint");
        try
        {
            return Results.Ok(await controlPlane.StopAsync(runId, cancellationToken));
        }
        catch (RealLoadTestNotFoundException)
        {
            logger.LogWarning("Real load test stop requested for missing runId={RunId}", runId);
            return Results.NotFound();
        }
    }

    private sealed record StartRealLoadTestRequest(
        string Scenario,
        string Runner,
        string Target,
        int Rate,
        int? PeakRate,
        int DurationSeconds,
        int MaxVus,
        int? StartVus,
        IReadOnlyList<RealLoadTestTargetSpec>? Targets);
}
