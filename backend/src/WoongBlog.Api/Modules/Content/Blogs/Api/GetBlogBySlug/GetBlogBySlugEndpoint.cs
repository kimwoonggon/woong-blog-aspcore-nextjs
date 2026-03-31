using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.GetBlogBySlug;

internal static class GetBlogBySlugEndpoint
{
    internal static void MapGetBlogBySlug(this IEndpointRouteBuilder app)
    {
        app.MapGet(BlogsApiPaths.GetBlogBySlug, async (
                string slug,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetBlogBySlugQuery(slug), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Blogs")
            .WithName("GetBlogBySlug")
            .Produces<BlogDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
