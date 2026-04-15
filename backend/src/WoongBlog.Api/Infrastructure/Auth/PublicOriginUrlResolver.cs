using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Infrastructure.Auth;

internal static class PublicOriginUrlResolver
{
    public static string ResolveCallbackUri(HttpRequest request, AuthOptions options)
    {
        var origin = string.IsNullOrWhiteSpace(options.PublicOrigin)
            ? GetRequestOrigin(request)
            : NormalizeOrigin(options.PublicOrigin);

        return $"{origin}{options.CallbackPath}";
    }

    public static string NormalizeOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Invalid public origin: {origin}");
        }

        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port);
        return builder.Uri.GetLeftPart(UriPartial.Authority);
    }

    private static string GetRequestOrigin(HttpRequest request)
    {
        var builder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1);
        return builder.Uri.GetLeftPart(UriPartial.Authority);
    }
}
