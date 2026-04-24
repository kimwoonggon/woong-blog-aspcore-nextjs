using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;

namespace WoongBlog.Api.Modules.Content.Blogs.GetBlogs;

internal static class GetBlogsEndpoint
{
    internal static void MapGetBlogs(this IEndpointRouteBuilder app)
    {
        app.MapGet(BlogsApiPaths.GetBlogs, async (
                [AsParameters] GetBlogsRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("Public Blogs")
            .WithName("GetBlogs")
            .Produces<PagedBlogsDto>(StatusCodes.Status200OK);
    }
}
