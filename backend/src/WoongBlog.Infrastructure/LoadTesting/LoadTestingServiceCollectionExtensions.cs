using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WoongBlog.Infrastructure.LoadTesting;

public static class LoadTestingServiceCollectionExtensions
{
    public static IServiceCollection AddLoadTestingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddOptions<LoadTestingOptions>()
            .Bind(configuration.GetSection(LoadTestingOptions.SectionName));

        services.AddSingleton<IRealLoadTestRunRegistry, InMemoryRealLoadTestRunRegistry>();
        services.AddSingleton<RealLoadTestReportStore>();
        services.AddSingleton<IRealLoadTestRunner, FakeRealLoadTestRunner>();
        services.AddSingleton<IRealLoadTestControlPlane, RealLoadTestControlPlane>();

        return services;
    }
}
