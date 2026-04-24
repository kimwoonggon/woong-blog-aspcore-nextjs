using MediatR;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Media.Commands.DeleteMediaAsset;

namespace WoongBlog.Api.Modules.Media.DeleteAsset;

internal static class DeleteAssetEndpoint
{
    internal static void MapDeleteAsset(this IEndpointRouteBuilder app)
    {
        app.MapDelete(MediaApiPaths.Uploads, async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var deleted = await sender.Send(new DeleteMediaAssetCommand(id), cancellationToken);
                return deleted.Found
                    ? Results.Ok(new { success = true })
                    : Results.NotFound(new { error = "Asset not found" });
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Media")
            .WithName("DeleteAsset");
    }
}
