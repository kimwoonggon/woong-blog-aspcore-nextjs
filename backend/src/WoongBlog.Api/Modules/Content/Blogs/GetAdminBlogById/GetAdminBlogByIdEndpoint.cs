using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogById;

namespace WoongBlog.Api.Modules.Content.Blogs.GetAdminBlogById;

internal static class GetAdminBlogByIdEndpoint
{
    internal static void MapGetAdminBlogById(this IEndpointRouteBuilder app)
    {
        app.MapGet(BlogsApiPaths.GetAdminBlogById, async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminBlogByIdQuery(id), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Blogs")
            .WithName("GetAdminBlogById")
            .Produces<AdminBlogDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
