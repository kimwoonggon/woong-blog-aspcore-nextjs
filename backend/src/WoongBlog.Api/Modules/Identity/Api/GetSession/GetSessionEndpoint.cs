using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WoongBlog.Api.Modules.Identity.Application;

namespace WoongBlog.Api.Modules.Identity.Api.GetSession;

internal static class GetSessionEndpoint
{
    internal static void MapGetSession(this IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityApiPaths.Session, (HttpContext httpContext) =>
            {
                var user = httpContext.User;
                if (!(user.Identity?.IsAuthenticated ?? false))
                {
                    return Results.Ok(new { authenticated = false });
                }

                return Results.Ok(new
                {
                    authenticated = true,
                    name = user.Identity?.Name,
                    email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email"),
                    role = user.FindFirstValue(AuthClaimTypes.Role),
                    profileId = user.FindFirstValue(AuthClaimTypes.ProfileId)
                });
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithName("AuthSession");
    }
}
