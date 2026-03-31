using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Blogs.Application.DeleteBlog;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.DeleteBlog;

internal static class DeleteBlogEndpoint
{
    internal static void MapDeleteBlog(this IEndpointRouteBuilder app)
    {
        app.MapDelete(BlogsApiPaths.DeleteBlog, async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var deleted = await sender.Send(new DeleteBlogCommand(id), cancellationToken);
                return deleted.Found ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Blogs")
            .WithName("DeleteBlog")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
