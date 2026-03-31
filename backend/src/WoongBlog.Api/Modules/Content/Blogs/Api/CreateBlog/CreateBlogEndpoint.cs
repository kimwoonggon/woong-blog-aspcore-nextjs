using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.CreateBlog;

internal static class CreateBlogEndpoint
{
    internal static void MapCreateBlog(this IEndpointRouteBuilder app)
    {
        app.MapPost(BlogsApiPaths.CreateBlog, async (
                CreateBlogRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return Results.Ok(new { result.Id, result.Slug });
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<CreateBlogRequest>()
            .WithTags("Admin Blogs")
            .WithName("CreateBlog")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
