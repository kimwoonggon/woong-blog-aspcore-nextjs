using System.Net;
using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.Proxy;

internal sealed class ProxyOptionsValidator : IValidateOptions<ProxyOptions>
{
    public ValidateOptionsResult Validate(string? name, ProxyOptions options)
    {
        var failures = new List<string>();

        if (options.ForwardLimit is <= 0)
        {
            failures.Add("Proxy:ForwardLimit must be greater than 0 when provided.");
        }

        foreach (var proxy in options.KnownProxies)
        {
            if (!IPAddress.TryParse(proxy, out _))
            {
                failures.Add($"Proxy:KnownProxies contains an invalid IP address: {proxy}");
            }
        }

        foreach (var network in options.KnownNetworks)
        {
            var parts = network.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !IPAddress.TryParse(parts[0], out _)
                || !int.TryParse(parts[1], out var prefixLength)
                || prefixLength < 0
                || prefixLength > 128)
            {
                failures.Add($"Proxy:KnownNetworks contains an invalid CIDR entry: {network}");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
