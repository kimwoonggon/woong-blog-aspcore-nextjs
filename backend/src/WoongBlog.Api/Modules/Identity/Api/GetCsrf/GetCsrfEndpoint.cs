using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Security;

namespace WoongBlog.Api.Modules.Identity.Api.GetCsrf;

internal static class GetCsrfEndpoint
{
    internal static void MapGetCsrf(this IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityApiPaths.Csrf, (
                HttpContext httpContext,
                IAntiforgery antiforgery,
                IOptions<SecurityOptions> securityOptions) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(httpContext);
                return Results.Ok(new
                {
                    requestToken = tokens.RequestToken,
                    headerName = securityOptions.Value.AntiforgeryHeaderName
                });
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithName("AuthCsrf");
    }
}
