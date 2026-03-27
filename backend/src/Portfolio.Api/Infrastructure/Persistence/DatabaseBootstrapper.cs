using Portfolio.Api.Infrastructure.Persistence.Seeding;

namespace Portfolio.Api.Infrastructure.Persistence;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(PortfolioDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Keep startup behavior centralized so the eventual migration-based path
        // can replace this in one place without changing the application entrypoint.
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                await SeedData.InitializeAsync(dbContext);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
