using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Public.Abstractions;
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

        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminMemberService, AdminMemberService>();
        services.AddScoped<IAdminPageService, AdminPageService>();
        services.AddScoped<IAdminSiteSettingsService, AdminSiteSettingsService>();
        services.AddScoped<IAdminBlogService, AdminBlogService>();
        services.AddScoped<IAdminWorkService, AdminWorkService>();
        services.AddScoped<IPublicHomeService, PublicHomeService>();
        services.AddScoped<IPublicPageService, PublicPageService>();
        services.AddScoped<IPublicSiteService, PublicSiteService>();
        services.AddScoped<IPublicBlogService, PublicBlogService>();
        services.AddScoped<IPublicWorkService, PublicWorkService>();

        return services;
    }
}
