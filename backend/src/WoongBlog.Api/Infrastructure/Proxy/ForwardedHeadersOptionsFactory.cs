using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace WoongBlog.Api.Infrastructure.Proxy;

internal static class ForwardedHeadersOptionsFactory
{
    public static void Apply(ForwardedHeadersOptions options, ProxyOptions proxyOptions)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.ForwardLimit = proxyOptions.ForwardLimit;
        options.RequireHeaderSymmetry = proxyOptions.RequireHeaderSymmetry;

        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        if (proxyOptions.KnownProxies.Length == 0 && proxyOptions.KnownNetworks.Length == 0)
        {
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            return;
        }

        foreach (var knownProxy in proxyOptions.KnownProxies)
        {
            if (IPAddress.TryParse(knownProxy, out var proxyAddress))
            {
                options.KnownProxies.Add(proxyAddress);
            }
        }

        foreach (var knownNetwork in proxyOptions.KnownNetworks)
        {
            var parts = knownNetwork.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2
                && IPAddress.TryParse(parts[0], out var prefixAddress)
                && int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse($"{prefixAddress}/{prefixLength}"));
            }
        }
    }
}
