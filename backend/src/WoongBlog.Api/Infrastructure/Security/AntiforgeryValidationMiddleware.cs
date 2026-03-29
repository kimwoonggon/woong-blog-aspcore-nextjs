using Microsoft.AspNetCore.Antiforgery;

namespace WoongBlog.Api.Infrastructure.Security;

public class AntiforgeryValidationMiddleware
{
    private readonly RequestDelegate _next;

    public AntiforgeryValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IAntiforgery antiforgery)
    {
        if (!RequiresValidation(context))
        {
            await _next(context);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid or missing CSRF token."
            });
            return;
        }

        await _next(context);
    }

    private static bool RequiresValidation(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsDelete(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method))
        {
            return false;
        }

        return context.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase)
               || context.Request.Path.StartsWithSegments("/api/uploads", StringComparison.OrdinalIgnoreCase)
               || context.Request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase);
    }
}
