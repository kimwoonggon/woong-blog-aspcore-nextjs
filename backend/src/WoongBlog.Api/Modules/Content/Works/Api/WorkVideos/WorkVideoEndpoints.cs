using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api;
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
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new IssueWorkVideoUploadCommand(
                    id,
                    request.FileName,
                    request.ContentType,
                    request.Size,
                    request.ExpectedVideosVersion), cancellationToken);

                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<IssueWorkVideoUploadRequest>()
            .WithTags("Admin Work Videos")
            .WithName("IssueWorkVideoUpload");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/upload", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(httpContext.Request.Query["uploadSessionId"], out var uploadSessionId))
                {
                    return Results.BadRequest(new { error = "uploadSessionId is required." });
                }

                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                var result = await sender.Send(new UploadLocalWorkVideoCommand(id, uploadSessionId, FormFileUpload.From(form.Files["file"])), cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Work Videos")
            .WithName("UploadLocalWorkVideo");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/hls-job", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                if (!int.TryParse(form["expectedVideosVersion"], out var expectedVideosVersion))
                {
                    return Results.BadRequest(new { error = "expectedVideosVersion is required." });
                }

                var result = await sender.Send(new StartWorkVideoHlsJobCommand(
                    id,
                    FormFileUpload.From(form.Files["file"]),
                    expectedVideosVersion), cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Work Videos")
            .WithName("StartWorkVideoHlsJob");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/confirm", async (
                Guid id,
                ConfirmWorkVideoUploadRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ConfirmWorkVideoUploadCommand(
                    id,
                    request.UploadSessionId,
                    request.ExpectedVideosVersion), cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<ConfirmWorkVideoUploadRequest>()
            .WithTags("Admin Work Videos")
            .WithName("ConfirmWorkVideoUpload");

        app.MapPost($"{WorksApiPaths.GetAdminWorkById}/videos/youtube", async (
                Guid id,
                AddYouTubeWorkVideoRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new AddYouTubeWorkVideoCommand(
                    id,
                    request.YoutubeUrlOrId,
                    request.ExpectedVideosVersion), cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<AddYouTubeWorkVideoRequest>()
            .WithTags("Admin Work Videos")
            .WithName("AddYouTubeWorkVideo");

        app.MapPut($"{WorksApiPaths.GetAdminWorkById}/videos/order", async (
                Guid id,
                ReorderWorkVideosRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ReorderWorkVideosCommand(
                    id,
                    request.OrderedVideoIds,
                    request.ExpectedVideosVersion), cancellationToken);
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
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteWorkVideoCommand(id, videoId, expectedVideosVersion), cancellationToken);
                return ToResult(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Work Videos")
            .WithName("DeleteWorkVideo");
    }

    private static IResult ToResult<T>(WorkVideoResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Status switch
        {
            WorkVideoResultStatus.NotFound => Results.NotFound(new { error = result.Error }),
            WorkVideoResultStatus.Conflict => Results.Conflict(new { error = result.Error }),
            _ => Results.BadRequest(new { error = result.Error })
        };
    }
}
