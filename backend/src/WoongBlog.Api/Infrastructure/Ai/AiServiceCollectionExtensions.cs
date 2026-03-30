using Microsoft.Extensions.Options;
using WoongBlog.Api.Endpoints;

namespace WoongBlog.Api.Infrastructure.Ai;

internal static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IPostConfigureOptions<AiOptions>>(_ => new AiOptionsPostConfigure(configuration));
        services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();
        services.AddHttpClient<IBlogAiFixService, BlogAiFixService>();
        services.AddScoped<IAdminAiWorkflowService, AdminAiWorkflowService>();
        services.AddHostedService<AiBatchJobProcessor>();

        return services;
    }
}
