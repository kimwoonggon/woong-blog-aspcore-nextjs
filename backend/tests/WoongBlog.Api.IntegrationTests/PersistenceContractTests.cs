using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Persistence.Seeding;

namespace WoongBlog.Api.Tests;

public class PersistenceContractTests
{
    private static IEntityType RequireEntityType(IModel model, Type entityType)
    {
        var resolvedEntityType = model.FindEntityType(entityType);
        return Assert.IsAssignableFrom<IEntityType>(resolvedEntityType);
    }

    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
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

        Assert.NotNull(worksEntity.FindProperty(nameof(Work.SearchTitle)));
        Assert.NotNull(worksEntity.FindProperty(nameof(Work.SearchText)));
        Assert.NotNull(blogsEntity.FindProperty(nameof(Blog.SearchTitle)));
        Assert.NotNull(blogsEntity.FindProperty(nameof(Blog.SearchText)));

        Assert.Contains(worksEntity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Work.Published), nameof(Work.PublishedAt)]));

        Assert.Contains(blogsEntity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Blog.Published), nameof(Blog.PublishedAt)]));

        Assert.Contains(worksEntity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Work.SearchTitle)]));

        Assert.Contains(blogsEntity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Blog.SearchTitle)]));

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
    public async Task SaveChanges_PopulatesContentSearchFields()
    {
        await using var dbContext = CreateDbContext();
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "T,B,N 안녕하세요",
            Slug = "blog-search",
            Excerpt = "Blog excerpt",
            ContentJson = "{\"markdown\":\"## Search Markdown\"}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = "Work Search Title",
            Slug = "work-search",
            Excerpt = "Work excerpt",
            Category = "case-study",
            ContentJson = "{\"html\":\"<p>Search HTML</p>\"}",
            AllPropertiesJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Blogs.Add(blog);
        dbContext.Works.Add(work);
        await dbContext.SaveChangesAsync();

        Assert.Equal("tbn안녕하세요", blog.SearchTitle);
        Assert.Contains("blogexcerpt", blog.SearchText, StringComparison.Ordinal);
        Assert.Contains("searchmarkdown", blog.SearchText, StringComparison.Ordinal);
        Assert.Equal("worksearchtitle", work.SearchTitle);
        Assert.Contains("workexcerpt", work.SearchText, StringComparison.Ordinal);
        Assert.Contains("searchhtml", work.SearchText, StringComparison.Ordinal);
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
        Assert.True(dbContext.Works.Count() >= 20);
        Assert.True(dbContext.Blogs.Count() >= 20);
        Assert.Equal(6, dbContext.Assets.Count());

        var homePage = dbContext.Pages.Single(page => page.Slug == "home");
        var siteSettings = dbContext.SiteSettings.Single(setting => setting.Singleton);
        var resumeAsset = dbContext.Assets.Single(asset => asset.Id == siteSettings.ResumeAssetId);

        Assert.Contains("headline", homePage.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/media/resume/woonggon-kim-resume.pdf", resumeAsset.PublicUrl);
        Assert.NotNull(dbContext.Works.SingleOrDefault(work => work.Slug == "seeded-work"));
        Assert.NotNull(dbContext.Blogs.SingleOrDefault(blog => blog.Slug == "seeded-blog"));
        Assert.All(dbContext.Works, work => Assert.True(work.Published));
        Assert.All(dbContext.Blogs, blog => Assert.True(blog.Published));
    }
}
