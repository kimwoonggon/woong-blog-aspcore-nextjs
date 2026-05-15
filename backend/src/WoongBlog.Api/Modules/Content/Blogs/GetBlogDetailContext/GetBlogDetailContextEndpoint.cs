using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogDetailContext;

namespace WoongBlog.Api.Modules.Content.Blogs.GetBlogDetailContext;

internal static class GetBlogDetailContextEndpoint
{
    internal static void MapGetBlogDetailContext(this IEndpointRouteBuilder app)
    {
        app.MapGet(BlogsApiPaths.GetBlogDetailContext, async (
                string slug,
                int? limit,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetBlogDetailContextQuery(slug, limit ?? 9),
                    cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Blogs")
            .WithName("GetBlogDetailContext")
            .Produces<BlogDetailContextDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
