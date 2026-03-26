using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Portfolio.Api.Domain.Entities;
using Portfolio.Api.Infrastructure.Persistence;
using Portfolio.Api.Infrastructure.Persistence.Seeding;

namespace Portfolio.Api.Tests;

public class PersistenceContractTests
{
    private static IEntityType RequireEntityType(IModel model, Type entityType)
    {
        var resolvedEntityType = model.FindEntityType(entityType);
        return Assert.IsAssignableFrom<IEntityType>(resolvedEntityType);
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PortfolioDbContext(options);
    }

    [Fact]
    public void ModelConfiguration_KeepsExpectedJsonAndUniquenessContracts()
    {
        using var dbContext = CreateDbContext();
        var model = dbContext.Model;

        var pagesEntity = RequireEntityType(model, typeof(PageEntity));
        var worksEntity = RequireEntityType(model, typeof(Work));
        var blogsEntity = RequireEntityType(model, typeof(Blog));
        var profilesEntity = RequireEntityType(model, typeof(Profile));
        var authSessionsEntity = RequireEntityType(model, typeof(AuthSession));
        var siteSettingsEntity = RequireEntityType(model, typeof(SiteSetting));

        Assert.Equal("jsonb", pagesEntity.FindProperty(nameof(PageEntity.ContentJson))!.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value);
        Assert.Equal("jsonb", worksEntity.FindProperty(nameof(Work.ContentJson))!.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value);
        Assert.Equal("jsonb", worksEntity.FindProperty(nameof(Work.AllPropertiesJson))!.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value);
        Assert.Equal("jsonb", blogsEntity.FindProperty(nameof(Blog.ContentJson))!.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value);

        Assert.Contains(worksEntity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Work.Slug)]));

        Assert.Contains(blogsEntity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Blog.Slug)]));

        Assert.Contains(profilesEntity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Profile.Provider), nameof(Profile.ProviderSubject)]));

        Assert.Contains(authSessionsEntity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(AuthSession.SessionKey)]));

        var primaryKey = siteSettingsEntity.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(SiteSetting.Singleton), primaryKey!.Properties.Single().Name);
    }

    [Fact]
    public async Task SeedData_SeedsCoreContractData_OnlyOnce()
    {
        await using var dbContext = CreateDbContext();

        await SeedData.InitializeAsync(dbContext);
        await SeedData.InitializeAsync(dbContext);

        Assert.Single(dbContext.SiteSettings);
        Assert.Equal(2, dbContext.Profiles.Count());
        Assert.Equal(3, dbContext.Pages.Count());
        Assert.Equal(2, dbContext.Works.Count());
        Assert.Equal(2, dbContext.Blogs.Count());
        Assert.Equal(6, dbContext.Assets.Count());

        var homePage = dbContext.Pages.Single(page => page.Slug == "home");
        var siteSettings = dbContext.SiteSettings.Single(setting => setting.Singleton);
        var resumeAsset = dbContext.Assets.Single(asset => asset.Id == siteSettings.ResumeAssetId);

        Assert.Contains("headline", homePage.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/media/resume/woonggon-kim-resume.pdf", resumeAsset.PublicUrl);
        Assert.All(dbContext.Works, work => Assert.True(work.Published));
        Assert.All(dbContext.Blogs, blog => Assert.True(blog.Published));
    }
}
