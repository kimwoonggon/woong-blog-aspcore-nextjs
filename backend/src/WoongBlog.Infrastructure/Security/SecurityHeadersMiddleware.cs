namespace WoongBlog.Infrastructure.Security;

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
            var isMediaRequest = context.Request.Path.StartsWithSegments("/media", StringComparison.OrdinalIgnoreCase);
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
            headers["X-Frame-Options"] = isMediaRequest ? "SAMEORIGIN" : "DENY";
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
            headers["Content-Security-Policy"] = isMediaRequest
                ? AllowSameOriginFrames(_options.ContentSecurityPolicy)
                : _options.ContentSecurityPolicy;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string AllowSameOriginFrames(string contentSecurityPolicy)
    {
        const string denyDirective = "frame-ancestors 'none'";
        const string sameOriginDirective = "frame-ancestors 'self'";

        return contentSecurityPolicy.Contains(denyDirective, StringComparison.OrdinalIgnoreCase)
            ? contentSecurityPolicy.Replace(denyDirective, sameOriginDirective, StringComparison.OrdinalIgnoreCase)
            : $"{contentSecurityPolicy.TrimEnd().TrimEnd(';')}; {sameOriginDirective};";
    }
}
