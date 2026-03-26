namespace Portfolio.Api.Infrastructure.Security;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, Microsoft.Extensions.Options.IOptions<SecurityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
            headers["X-Frame-Options"] = "DENY";
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
