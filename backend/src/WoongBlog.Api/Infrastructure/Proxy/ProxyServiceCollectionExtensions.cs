using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Infrastructure.Proxy;

internal static class ProxyServiceCollectionExtensions
{
    public static IServiceCollection AddProxyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ProxyOptions>()
            .Bind(configuration.GetSection(ProxyOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ProxyOptions>, ProxyOptionsValidator>();

        var proxyOptions = configuration.GetSection(ProxyOptions.SectionName).Get<ProxyOptions>() ?? new ProxyOptions();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            ForwardedHeadersOptionsFactory.Apply(options, proxyOptions);
        });

        return services;
    }
}
