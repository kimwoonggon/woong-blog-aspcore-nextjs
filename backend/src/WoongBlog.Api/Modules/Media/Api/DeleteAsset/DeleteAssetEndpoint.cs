using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Media.Application;

namespace WoongBlog.Api.Modules.Media.Api.DeleteAsset;

internal static class DeleteAssetEndpoint
{
    internal static void MapDeleteAsset(this IEndpointRouteBuilder app)
    {
        app.MapDelete(MediaApiPaths.Uploads, async (
                Guid id,
                IMediaAssetService mediaAssetService,
                CancellationToken cancellationToken) =>
            {
                var deleted = await mediaAssetService.DeleteAsync(id, cancellationToken);
                return deleted.Found
                    ? Results.Ok(new { success = true })
                    : Results.NotFound(new { error = "Asset not found" });
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Media")
            .WithName("DeleteAsset");
    }
}
