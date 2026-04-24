using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api;
using WoongBlog.Application.Modules.Media.Commands.UploadMediaAsset;
using WoongBlog.Application.Modules.Media.Results;

namespace WoongBlog.Api.Modules.Media.UploadAsset;

internal static class UploadAssetEndpoint
{
    internal static void MapUploadAsset(this IEndpointRouteBuilder app)
    {
        app.MapPost(MediaApiPaths.Uploads, async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var formData = await httpContext.Request.ReadFormAsync(cancellationToken);
                var result = await sender.Send(new UploadMediaAssetCommand(
                    FormFileUpload.From(formData.Files["file"]),
                    formData["bucket"].ToString(),
                    httpContext.User), cancellationToken);

                if (!result.Success)
                {
                    return Results.Json(new { error = result.Error }, statusCode: ToStatusCode(result.Status));
                }

                return Results.Ok(new
                {
                    id = result.AssetId,
                    url = result.PublicUrl,
                    path = result.Path
                });
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Media")
            .WithName("UploadAsset");
    }

    private static int ToStatusCode(MediaUploadStatus status) => status switch
    {
        MediaUploadStatus.BadRequest => StatusCodes.Status400BadRequest,
        MediaUploadStatus.Failed => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status200OK
    };
}
