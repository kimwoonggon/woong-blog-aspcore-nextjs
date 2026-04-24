using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Works.CreateWork;
using WoongBlog.Api.Modules.Content.Works.DeleteWork;
using WoongBlog.Api.Modules.Content.Works.GetAdminWorkById;
using WoongBlog.Api.Modules.Content.Works.GetAdminWorks;
using WoongBlog.Api.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Api.Modules.Content.Works.GetWorks;
using WoongBlog.Api.Modules.Content.Works.UpdateWork;
using WoongBlog.Api.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works;

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
