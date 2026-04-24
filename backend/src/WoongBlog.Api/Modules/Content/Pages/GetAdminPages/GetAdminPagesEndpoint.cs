using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Pages.GetAdminPages;

namespace WoongBlog.Api.Modules.Content.Pages.GetAdminPages;

internal static class GetAdminPagesEndpoint
{
    internal static void MapGetAdminPages(this IEndpointRouteBuilder app)
    {
        app.MapGet(PagesApiPaths.GetAdminPages, async (
                [AsParameters] GetAdminPagesRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminPagesQuery(request.Slugs), cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Pages")
            .WithName("GetAdminPages")
            .Produces<IReadOnlyList<AdminPageListItemDto>>(StatusCodes.Status200OK);
    }
}
