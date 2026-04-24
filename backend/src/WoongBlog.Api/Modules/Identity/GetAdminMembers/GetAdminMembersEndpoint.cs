using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Identity.GetAdminMembers;

namespace WoongBlog.Api.Modules.Identity.GetAdminMembers;

internal static class GetAdminMembersEndpoint
{
    internal static void MapGetAdminMembers(this IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityApiPaths.Members, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminMembersQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Members")
            .WithName("GetAdminMembers")
            .Produces<IReadOnlyList<AdminMemberListItemDto>>(StatusCodes.Status200OK);
    }
}
