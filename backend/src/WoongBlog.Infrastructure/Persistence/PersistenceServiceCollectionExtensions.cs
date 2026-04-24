using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WoongBlog.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
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

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(WoongBlogDbContext).Assembly.FullName));
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
