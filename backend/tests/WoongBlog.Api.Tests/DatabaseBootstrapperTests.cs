using Microsoft.EntityFrameworkCore;
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
        Assert.Equal(2, dbContext.Works.Count());
        Assert.Equal(2, dbContext.Blogs.Count());
        Assert.Equal(6, dbContext.Assets.Count());
    }

    [Fact]
    public async Task InitializeAsync_Rehydrates_Public_Detail_Seeds_When_Runtime_Data_Already_Exists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SiteSettings.Add(new WoongBlog.Api.Domain.Entities.SiteSetting
        {
            Singleton = true,
            OwnerName = "Existing",
            Tagline = "Existing"
        });
        await dbContext.SaveChangesAsync();

        await DatabaseBootstrapper.InitializeAsync(dbContext);

        Assert.NotNull(await dbContext.Works.SingleOrDefaultAsync(x => x.Slug == "seeded-work"));
        Assert.NotNull(await dbContext.Blogs.SingleOrDefaultAsync(x => x.Slug == "seeded-blog"));
    }
}
