using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Pages.GetAdminPages;
using WoongBlog.Api.Modules.Content.Pages.GetPageBySlug;
using WoongBlog.Api.Modules.Content.Pages.UpdatePage;

namespace WoongBlog.Api.Modules.Content.Pages;

internal static class PagesEndpoints
{
    internal static void MapPagesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetAdminPages();
        app.MapUpdatePage();
        app.MapGetPageBySlug();
    }
}
