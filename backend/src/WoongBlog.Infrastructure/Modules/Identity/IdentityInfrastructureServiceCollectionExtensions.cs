using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Modules.Identity.Application.Abstractions;
using WoongBlog.Api.Modules.Identity.Infrastructure;
using WoongBlog.Api.Modules.Identity.Persistence;

namespace WoongBlog.Api.Modules.Identity;

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
