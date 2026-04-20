using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Media.Application.Commands.UploadMediaAsset;

namespace WoongBlog.Api.Modules.Media.Api.UploadAsset;

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
                    formData.Files["file"],
                    formData["bucket"].ToString(),
                    httpContext.User), cancellationToken);

                if (!result.Success)
                {
                    return Results.Json(new { error = result.Error }, statusCode: result.StatusCode);
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
}
