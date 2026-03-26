namespace Portfolio.Api.Infrastructure.Proxy;

public class ProxyOptions
{
    public const string SectionName = "Proxy";

    public int? ForwardLimit { get; set; } = 2;
    public bool RequireHeaderSymmetry { get; set; } = true;
    public string[] KnownProxies { get; set; } = [];
    public string[] KnownNetworks { get; set; } = [];
}
