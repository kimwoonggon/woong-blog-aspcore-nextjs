using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.UpdateBlog;

internal static class UpdateBlogEndpoint
{
    internal static void MapUpdateBlog(this IEndpointRouteBuilder app)
    {
        app.MapPut(BlogsApiPaths.UpdateBlog, async (
                Guid id,
                UpdateBlogRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(new { result.Id, result.Slug });
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<UpdateBlogRequest>()
            .WithTags("Admin Blogs")
            .WithName("UpdateBlog")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
