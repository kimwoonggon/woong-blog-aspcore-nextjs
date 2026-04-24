using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogs;

namespace WoongBlog.Api.Modules.Content.Blogs.GetAdminBlogs;

internal static class GetAdminBlogsEndpoint
{
    internal static void MapGetAdminBlogs(this IEndpointRouteBuilder app)
    {
        app.MapGet(BlogsApiPaths.GetAdminBlogs, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminBlogsQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Blogs")
            .WithName("GetAdminBlogs")
            .Produces<IReadOnlyList<AdminBlogListItemDto>>(StatusCodes.Status200OK);
    }
}
