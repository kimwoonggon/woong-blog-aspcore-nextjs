using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Media.DeleteAsset;
using WoongBlog.Api.Modules.Media.UploadAsset;

namespace WoongBlog.Api.Modules.Media;

internal static class MediaEndpoints
{
    internal static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapUploadAsset();
        app.MapDeleteAsset();
    }
}
