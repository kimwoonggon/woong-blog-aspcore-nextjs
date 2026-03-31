using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Media.Api.DeleteAsset;
using WoongBlog.Api.Modules.Media.Api.UploadAsset;

namespace WoongBlog.Api.Modules.Media.Api;

internal static class MediaEndpoints
{
    internal static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapUploadAsset();
        app.MapDeleteAsset();
    }
}
