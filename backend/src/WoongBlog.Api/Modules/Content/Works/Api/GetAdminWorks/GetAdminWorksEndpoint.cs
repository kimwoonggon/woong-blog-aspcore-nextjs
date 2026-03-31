using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;

namespace WoongBlog.Api.Modules.Content.Works.Api.GetAdminWorks;

internal static class GetAdminWorksEndpoint
{
    internal static void MapGetAdminWorks(this IEndpointRouteBuilder app)
    {
        app.MapGet(WorksApiPaths.GetAdminWorks, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminWorksQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Works")
            .WithName("GetAdminWorks")
            .Produces<IReadOnlyList<AdminWorkListItemDto>>(StatusCodes.Status200OK);
    }
}
