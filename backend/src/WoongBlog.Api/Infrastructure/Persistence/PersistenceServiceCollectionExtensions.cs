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
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminMemberService, AdminMemberService>();
        services.AddScoped<AdminPageService>();
        services.AddScoped<IAdminPageQueries>(sp => sp.GetRequiredService<AdminPageService>());
        services.AddScoped<IAdminPageWriteStore>(sp => sp.GetRequiredService<AdminPageService>());
        services.AddScoped<AdminSiteSettingsService>();
        services.AddScoped<IAdminSiteSettingsQueries>(sp => sp.GetRequiredService<AdminSiteSettingsService>());
        services.AddScoped<IAdminSiteSettingsWriteStore>(sp => sp.GetRequiredService<AdminSiteSettingsService>());
        services.AddScoped<AdminBlogService>();
        services.AddScoped<IAdminBlogQueries>(sp => sp.GetRequiredService<AdminBlogService>());
        services.AddScoped<IAdminBlogWriteStore>(sp => sp.GetRequiredService<AdminBlogService>());
        services.AddScoped<AdminWorkService>();
        services.AddScoped<IAdminWorkQueries>(sp => sp.GetRequiredService<AdminWorkService>());
        services.AddScoped<IAdminWorkWriteStore>(sp => sp.GetRequiredService<AdminWorkService>());
        services.AddScoped<IPublicHomeService, PublicHomeService>();
        services.AddScoped<IPublicPageService, PublicPageService>();
        services.AddScoped<IPublicSiteService, PublicSiteService>();
        services.AddScoped<IPublicBlogService, PublicBlogService>();
        services.AddScoped<IPublicWorkService, PublicWorkService>();

        return services;
    }
}
