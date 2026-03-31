using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI;

internal static class AiModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAiInfrastructure(configuration);
        services.AddScoped<IAiAdminService, AiAdminService>();
        return services;
    }
}
