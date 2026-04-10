using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Persistence;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Persistence;
using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Pages.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Persistence;
using WoongBlog.Api.Modules.Identity.Application.Abstractions;
using WoongBlog.Api.Modules.Identity.Persistence;
using WoongBlog.Api.Modules.Site.Application.Abstractions;
using WoongBlog.Api.Modules.Site.Persistence;

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
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IAdminPageService, AdminPageService>();
        services.AddScoped<IPublicPageService, PublicPageService>();
        services.AddScoped<IAdminBlogService, AdminBlogService>();
        services.AddScoped<IPublicBlogService, PublicBlogService>();
        services.AddScoped<IAdminWorkService, AdminWorkService>();
        services.AddScoped<IPublicWorkService, PublicWorkService>();
        services.AddScoped<IAdminMemberService, AdminMemberService>();
        services.AddScoped<IAdminSiteSettingsService, AdminSiteSettingsService>();
        services.AddScoped<IPublicSiteService, PublicSiteService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IPublicHomeService, PublicHomeService>();

        return services;
    }
}
