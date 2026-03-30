using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public class DatabaseBootstrapperTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    [Fact]
    public async Task InitializeAsync_SeedsContractData_AndCanBeCalledTwice()
    {
        await using var dbContext = CreateDbContext();

        await DatabaseBootstrapper.InitializeAsync(dbContext);
        await DatabaseBootstrapper.InitializeAsync(dbContext);

        Assert.Single(dbContext.SiteSettings);
        Assert.Equal(2, dbContext.Profiles.Count());
        Assert.Equal(3, dbContext.Pages.Count());
        Assert.Equal(13, dbContext.Works.Count());
        Assert.Equal(24, dbContext.Blogs.Count());
        Assert.Equal(6, dbContext.Assets.Count());
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_LogsRetries_AndEventualSuccess()
    {
        var logger = new ListLogger();
        var attempts = 0;

        await DatabaseBootstrapper.ExecuteWithRetryAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException($"fail-{attempts}");
                }

                return Task.CompletedTask;
            },
            logger,
            maxAttempts: 3,
            retryDelay: TimeSpan.Zero,
            cancellationToken: CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Warning));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("succeeded on attempt 3", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_LogsFinalFailure_AndRethrows()
    {
        var logger = new ListLogger();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DatabaseBootstrapper.ExecuteWithRetryAsync(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("boom");
                },
                logger,
                maxAttempts: 2,
                retryDelay: TimeSpan.Zero,
                cancellationToken: CancellationToken.None));

        Assert.Equal("boom", exception.Message);
        Assert.Equal(2, attempts);
        Assert.Single(logger.Entries.Where(entry => entry.Level == LogLevel.Warning));
        Assert.Single(logger.Entries.Where(entry => entry.Level == LogLevel.Error));
    }

    private sealed class ListLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
