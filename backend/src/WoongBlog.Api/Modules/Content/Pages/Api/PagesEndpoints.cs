using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Pages.Api.GetAdminPages;
using WoongBlog.Api.Modules.Content.Pages.Api.GetPageBySlug;
using WoongBlog.Api.Modules.Content.Pages.Api.UpdatePage;

namespace WoongBlog.Api.Modules.Content.Pages.Api;

internal static class PagesEndpoints
{
    internal static void MapPagesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetAdminPages();
        app.MapUpdatePage();
        app.MapGetPageBySlug();
    }
}
