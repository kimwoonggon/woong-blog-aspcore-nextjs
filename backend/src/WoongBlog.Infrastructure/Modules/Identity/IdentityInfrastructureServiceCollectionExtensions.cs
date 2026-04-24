using WoongBlog.Infrastructure.Auth;
using WoongBlog.Application.Modules.Identity.Abstractions;
using WoongBlog.Infrastructure.Modules.Identity.Services;
using WoongBlog.Infrastructure.Modules.Identity.Persistence;

namespace WoongBlog.Infrastructure.Modules.Identity;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddAuthInfrastructure(configuration, environment);
        services.AddScoped<IIdentityInteractionService, IdentityInteractionService>();
        services.AddScoped<IAdminMemberQueryStore, AdminMemberQueryStore>();
        return services;
    }
}
