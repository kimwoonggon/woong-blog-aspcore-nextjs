using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Threading.RateLimiting;

namespace WoongBlog.Api.Infrastructure.Security;

internal sealed class PublicReadRateLimiterPolicy(
    IOptionsMonitor<SecurityOptions> options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var securityOptions = options.CurrentValue;
        var partitionKey = $"public-read:{ResolveClientPartitionKey(httpContext)}";
        if (!securityOptions.PublicReadRateLimitEnabled)
        {
            return RateLimitPartition.GetNoLimiter(partitionKey);
        }

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = securityOptions.PublicReadRateLimitPermitLimit,
            Window = TimeSpan.FromSeconds(securityOptions.PublicReadRateLimitWindowSeconds),
            QueueLimit = 0
        });
    }

    private static string ResolveClientPartitionKey(HttpContext httpContext)
    {
        var cloudflareIp = FirstHeaderValue(httpContext.Request.Headers["CF-Connecting-IP"]);
        if (!string.IsNullOrWhiteSpace(cloudflareIp))
        {
            return cloudflareIp;
        }

        var forwardedFor = FirstForwardedForValue(httpContext.Request.Headers["X-Forwarded-For"]);
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string? FirstHeaderValue(StringValues values)
    {
        return values.Count > 0
            ? values[0]?.Trim()
            : null;
    }

    private static string? FirstForwardedForValue(StringValues values)
    {
        var header = FirstHeaderValue(values);
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var separatorIndex = header.IndexOf(',', StringComparison.Ordinal);
        return separatorIndex >= 0
            ? header[..separatorIndex].Trim()
            : header.Trim();
    }
}
