using Microsoft.Extensions.Options;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;

namespace WoongBlog.Api.Infrastructure.Ai;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IPostConfigureOptions<AiOptions>>(_ => new AiOptionsPostConfigure(configuration));
        services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();
        services.AddSingleton<AiBatchJobSignal>();
        services.AddSingleton<IAiBatchJobSignal>(serviceProvider => serviceProvider.GetRequiredService<AiBatchJobSignal>());
        services.AddSingleton<IAiRuntimeCapabilities, AiRuntimeCapabilities>();
        services.AddHttpClient<IBlogAiFixService, BlogAiFixService>();
        services.AddHostedService<AiBatchJobProcessor>();

        return services;
    }
}
