using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Infrastructure.Persistence.Assets;
using WoongBlog.Api.Infrastructure.Persistence.Admin;
using WoongBlog.Api.Infrastructure.Persistence.Public;

namespace WoongBlog.Api.Infrastructure.Persistence;

internal static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WoongBlogDbContext>(options =>
        {
            var databaseProvider = configuration["DatabaseProvider"];

            if (string.Equals(databaseProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                var inMemoryDatabaseName = configuration["InMemoryDatabaseName"] ?? "portfolio-tests";
                options.UseInMemoryDatabase(inMemoryDatabaseName);
                return;
            }

            var connectionString = configuration.GetConnectionString("Postgres")
                ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio";

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAssetStorageService, AssetStorageService>();
        services.AddScoped<IAdminDashboardQueries, AdminDashboardQueries>();
        services.AddScoped<IAdminMemberQueries, AdminMemberQueries>();
        services.AddScoped<AdminPagePersistence>();
        services.AddScoped<IAdminPageQueries>(sp => sp.GetRequiredService<AdminPagePersistence>());
        services.AddScoped<IAdminPageWriteStore>(sp => sp.GetRequiredService<AdminPagePersistence>());
        services.AddScoped<AdminSiteSettingsPersistence>();
        services.AddScoped<IAdminSiteSettingsQueries>(sp => sp.GetRequiredService<AdminSiteSettingsPersistence>());
        services.AddScoped<IAdminSiteSettingsWriteStore>(sp => sp.GetRequiredService<AdminSiteSettingsPersistence>());
        services.AddScoped<AdminBlogPersistence>();
        services.AddScoped<IAdminBlogQueries>(sp => sp.GetRequiredService<AdminBlogPersistence>());
        services.AddScoped<IAdminBlogWriteStore>(sp => sp.GetRequiredService<AdminBlogPersistence>());
        services.AddScoped<AdminWorkPersistence>();
        services.AddScoped<IAdminWorkQueries>(sp => sp.GetRequiredService<AdminWorkPersistence>());
        services.AddScoped<IAdminWorkWriteStore>(sp => sp.GetRequiredService<AdminWorkPersistence>());
        services.AddScoped<IPublicHomeQueries, PublicHomeQueries>();
        services.AddScoped<IPublicPageQueries, PublicPageQueries>();
        services.AddScoped<IPublicSiteQueries, PublicSiteQueries>();
        services.AddScoped<IPublicBlogQueries, PublicBlogQueries>();
        services.AddScoped<IPublicWorkQueries, PublicWorkQueries>();

        return services;
    }
}
