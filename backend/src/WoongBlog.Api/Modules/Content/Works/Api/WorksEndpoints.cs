using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Works.Api.CreateWork;
using WoongBlog.Api.Modules.Content.Works.Api.DeleteWork;
using WoongBlog.Api.Modules.Content.Works.Api.GetAdminWorkById;
using WoongBlog.Api.Modules.Content.Works.Api.GetAdminWorks;
using WoongBlog.Api.Modules.Content.Works.Api.GetWorkBySlug;
using WoongBlog.Api.Modules.Content.Works.Api.GetWorks;
using WoongBlog.Api.Modules.Content.Works.Api.UpdateWork;
using WoongBlog.Api.Modules.Content.Works.Api.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Api;

internal static class WorksEndpoints
{
    internal static void MapWorksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetAdminWorks();
        app.MapGetAdminWorkById();
        app.MapCreateWork();
        app.MapUpdateWork();
        app.MapDeleteWork();
        app.MapWorkVideoEndpoints();
        app.MapGetWorks();
        app.MapGetWorkBySlug();
    }
}
