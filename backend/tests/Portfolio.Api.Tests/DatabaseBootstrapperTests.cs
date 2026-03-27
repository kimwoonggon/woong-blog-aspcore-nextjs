using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Tests;

public class DatabaseBootstrapperTests
{
    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PortfolioDbContext(options);
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
        Assert.Equal(2, dbContext.Works.Count());
        Assert.Equal(2, dbContext.Blogs.Count());
        Assert.Equal(6, dbContext.Assets.Count());
    }
}
