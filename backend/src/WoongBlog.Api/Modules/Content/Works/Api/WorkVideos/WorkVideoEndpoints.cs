using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Api.WorkVideos;

internal static class WorkVideoEndpoints
{
    internal static void MapWorkVideoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/upload-url", async (
                Guid id,
                IssueWorkVideoUploadRequest request,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.IssueUploadAsync(
                    id,
                    request.FileName,
                    request.ContentType,
                    request.Size,
                    request.ExpectedVideosVersion,
                    cancellationToken);

                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<IssueWorkVideoUploadRequest>()
            .WithTags("Admin Work Videos")
            .WithName("IssueWorkVideoUpload");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/upload", async (
                Guid id,
                HttpContext httpContext,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(httpContext.Request.Query["uploadSessionId"], out var uploadSessionId))
                {
                    return Results.BadRequest(new { error = "uploadSessionId is required." });
                }

                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                var result = await service.UploadLocalAsync(id, uploadSessionId, form.Files["file"], cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Work Videos")
            .WithName("UploadLocalWorkVideo");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/hls-job", async (
                Guid id,
                HttpContext httpContext,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                if (!int.TryParse(form["expectedVideosVersion"], out var expectedVideosVersion))
                {
                    return Results.BadRequest(new { error = "expectedVideosVersion is required." });
                }

                var result = await service.UploadHlsAsync(id, form.Files["file"], expectedVideosVersion, cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Work Videos")
            .WithName("StartWorkVideoHlsJob");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/confirm", async (
                Guid id,
                ConfirmWorkVideoUploadRequest request,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.ConfirmUploadAsync(id, request.UploadSessionId, request.ExpectedVideosVersion, cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<ConfirmWorkVideoUploadRequest>()
            .WithTags("Admin Work Videos")
            .WithName("ConfirmWorkVideoUpload");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/youtube", async (
                Guid id,
                AddYouTubeWorkVideoRequest request,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AddYouTubeAsync(id, request.YoutubeUrlOrId, request.ExpectedVideosVersion, cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<AddYouTubeWorkVideoRequest>()
            .WithTags("Admin Work Videos")
            .WithName("AddYouTubeWorkVideo");

        app.MapPut($"{WorksApiPaths.GetAdminWorkById}/videos/order", async (
                Guid id,
                ReorderWorkVideosRequest request,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.ReorderAsync(id, request.OrderedVideoIds, request.ExpectedVideosVersion, cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<ReorderWorkVideosRequest>()
            .WithTags("Admin Work Videos")
            .WithName("ReorderWorkVideos");

        app.MapDelete($"{WorksApiPaths.GetAdminWorkById}/videos/{{videoId:guid}}", async (
                Guid id,
                Guid videoId,
                int expectedVideosVersion,
                IWorkVideoService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.DeleteAsync(id, videoId, expectedVideosVersion, cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Work Videos")
            .WithName("DeleteWorkVideo");
    }

    private static IResult ToResult<T>(WorkVideoServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.StatusCode switch
        {
            StatusCodes.Status404NotFound => Results.NotFound(new { error = result.Error }),
            StatusCodes.Status409Conflict => Results.Conflict(new { error = result.Error }),
            _ => Results.BadRequest(new { error = result.Error })
        };
    }
}
