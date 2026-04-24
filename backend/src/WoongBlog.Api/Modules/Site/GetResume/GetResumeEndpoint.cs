using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Site.GetResume;

namespace WoongBlog.Api.Modules.Site.GetResume;

internal static class GetResumeEndpoint
{
    internal static void MapGetResume(this IEndpointRouteBuilder app)
    {
        app.MapGet(SiteApiPaths.GetResume, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetResumeQuery(), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Site")
            .WithName("GetResume")
            .Produces<ResumeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
