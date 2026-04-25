using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;

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
        Assert.True(dbContext.Works.Count() >= 20);
        Assert.Equal(2, dbContext.WorkVideos.Count());
        Assert.True(dbContext.Blogs.Count() >= 20);
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
        Assert.Equal(2, await dbContext.WorkVideos.CountAsync());
        Assert.NotNull(await dbContext.Blogs.SingleOrDefaultAsync(x => x.Slug == "seeded-blog"));
    }

    [Fact]
    public async Task InitializeAsync_Reuses_Existing_Seeded_WorkVideo_Slots_When_Runtime_Data_Already_Exists()
    {
        await using var dbContext = CreateDbContext();

        var seededWorkId = Guid.NewGuid();
        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Existing",
            Tagline = "Existing"
        });
        dbContext.Works.Add(new Work
        {
            Id = seededWorkId,
            Slug = "seeded-work",
            Title = "Portfolio Platform Rebuild",
            Excerpt = "Existing seeded work"
        });
        dbContext.WorkVideos.AddRange(
            new WorkVideo
            {
                Id = Guid.NewGuid(),
                WorkId = seededWorkId,
                SortOrder = 0,
                SourceType = "legacy",
                SourceKey = "legacy-0",
                OriginalFileName = "Legacy 0"
            },
            new WorkVideo
            {
                Id = Guid.NewGuid(),
                WorkId = seededWorkId,
                SortOrder = 1,
                SourceType = "legacy",
                SourceKey = "legacy-1",
                OriginalFileName = "Legacy 1"
            });
        await dbContext.SaveChangesAsync();

        await DatabaseBootstrapper.InitializeAsync(dbContext);

        var seededVideos = await dbContext.WorkVideos
            .Where(x => x.WorkId == seededWorkId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        Assert.Equal(2, seededVideos.Count);
        Assert.Equal("youtube", seededVideos[0].SourceType);
        Assert.Equal("dQw4w9WgXcQ", seededVideos[0].SourceKey);
        Assert.Equal("youtube", seededVideos[1].SourceType);
        Assert.Equal("M7lc1UVf-VE", seededVideos[1].SourceKey);
    }
}
