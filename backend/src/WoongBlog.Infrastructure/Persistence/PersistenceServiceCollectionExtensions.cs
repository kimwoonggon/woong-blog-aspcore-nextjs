using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WoongBlog.Infrastructure.Persistence.Diagnostics;

namespace WoongBlog.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const int DefaultDbContextPoolSize = 128;
    private const int MinimumDbContextPoolSize = 16;
    private const int MaximumDbContextPoolSize = 1024;

    public static IServiceCollection AddPersistenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseDiagnosticsOptions>()
            .Bind(configuration.GetSection(DatabaseDiagnosticsOptions.SectionName))
            .Validate(
                options => options.LatencySampleCapacity > 0
                    && options.SlowQuerySampleCapacity > 0
                    && options.SlowQueryThresholdMs > 0,
                "Load test database diagnostics options must use positive values.");

        services.AddSingleton<IDatabaseDiagnosticsCollector, DatabaseDiagnosticsCollector>();
        services.AddSingleton<LoadTestDbCommandDiagnosticsInterceptor>();
        services.AddSingleton<LoadTestDbConnectionDiagnosticsInterceptor>();

        services.AddDbContextPool<WoongBlogDbContext>(
            (serviceProvider, options) =>
            {
                var databaseProvider = configuration["DatabaseProvider"];
                var commandDiagnosticsInterceptor = serviceProvider.GetRequiredService<LoadTestDbCommandDiagnosticsInterceptor>();
                var connectionDiagnosticsInterceptor = serviceProvider.GetRequiredService<LoadTestDbConnectionDiagnosticsInterceptor>();
                options.AddInterceptors(commandDiagnosticsInterceptor, connectionDiagnosticsInterceptor);

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
            },
            ResolveDbContextPoolSize(configuration));

        return services;
    }

    private static int ResolveDbContextPoolSize(IConfiguration configuration)
    {
        var configuredPoolSize = configuration.GetValue<int?>("Persistence:DbContextPoolSize");
        return Math.Clamp(
            configuredPoolSize ?? DefaultDbContextPoolSize,
            MinimumDbContextPoolSize,
            MaximumDbContextPoolSize);
    }
}
