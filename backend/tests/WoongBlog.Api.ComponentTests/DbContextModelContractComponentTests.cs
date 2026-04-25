using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class DbContextModelContractComponentTests
{
    [Fact]
    public void DbContext_ModelIncludesExpectedAggregateSetsAndKeys()
    {
        using var dbContext = CreateDbContext();
        var model = dbContext.Model;

        Assert.NotNull(RequireEntityType(model, typeof(Asset)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(AiBatchJob)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(AiBatchJobItem)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(AuthAuditLog)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(AuthSession)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(Blog)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(PageEntity)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(PageView)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(Profile)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(SchemaPatch)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(SiteSetting)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(VideoStorageCleanupJob)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(Work)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(WorkVideo)).FindPrimaryKey());
        Assert.NotNull(RequireEntityType(model, typeof(WorkVideoUploadSession)).FindPrimaryKey());
    }

    [Fact]
    public void DbContext_ModelDefinesRequiredJsonIndexAndUniquenessContracts()
    {
        using var dbContext = CreateDbContext();
        var model = dbContext.Model;
        var pages = RequireEntityType(model, typeof(PageEntity));
        var blogs = RequireEntityType(model, typeof(Blog));
        var works = RequireEntityType(model, typeof(Work));
        var profiles = RequireEntityType(model, typeof(Profile));
        var sessions = RequireEntityType(model, typeof(AuthSession));
        var jobItems = RequireEntityType(model, typeof(AiBatchJobItem));
        var siteSettings = RequireEntityType(model, typeof(SiteSetting));

        AssertPropertyRequired(pages, nameof(PageEntity.Slug));
        AssertPropertyRequired(pages, nameof(PageEntity.Title));
        AssertPropertyRequired(pages, nameof(PageEntity.ContentJson));
        AssertPropertyRequired(blogs, nameof(Blog.Slug));
        AssertPropertyRequired(blogs, nameof(Blog.Title));
        AssertPropertyRequired(blogs, nameof(Blog.ContentJson));
        AssertPropertyRequired(blogs, nameof(Blog.SearchTitle));
        AssertPropertyRequired(blogs, nameof(Blog.SearchText));
        AssertPropertyRequired(works, nameof(Work.Slug));
        AssertPropertyRequired(works, nameof(Work.Title));
        AssertPropertyRequired(works, nameof(Work.Category));
        AssertPropertyRequired(works, nameof(Work.ContentJson));
        AssertPropertyRequired(works, nameof(Work.AllPropertiesJson));
        AssertPropertyRequired(works, nameof(Work.SearchTitle));
        AssertPropertyRequired(works, nameof(Work.SearchText));

        Assert.Equal("jsonb", GetColumnType(pages, nameof(PageEntity.ContentJson)));
        Assert.Equal("jsonb", GetColumnType(blogs, nameof(Blog.ContentJson)));
        Assert.Equal("jsonb", GetColumnType(works, nameof(Work.ContentJson)));
        Assert.Equal("jsonb", GetColumnType(works, nameof(Work.AllPropertiesJson)));

        AssertHasUniqueIndex(pages, nameof(PageEntity.Slug));
        AssertHasUniqueIndex(blogs, nameof(Blog.Slug));
        AssertHasUniqueIndex(works, nameof(Work.Slug));
        AssertHasIndex(blogs, nameof(Blog.Published), nameof(Blog.PublishedAt));
        AssertHasIndex(works, nameof(Work.Published), nameof(Work.PublishedAt));
        AssertHasIndex(blogs, nameof(Blog.SearchTitle));
        AssertHasIndex(works, nameof(Work.SearchTitle));
        AssertHasUniqueIndex(profiles, nameof(Profile.Provider), nameof(Profile.ProviderSubject));
        AssertHasUniqueIndex(sessions, nameof(AuthSession.SessionKey));
        AssertHasUniqueIndex(jobItems, nameof(AiBatchJobItem.JobId), nameof(AiBatchJobItem.EntityId));

        var primaryKey = siteSettings.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(SiteSetting.Singleton), primaryKey!.Properties.Single().Name);
    }

    [Fact]
    public void DbContext_ModelDefinesExpectedWorkVideoCascadeContracts()
    {
        using var dbContext = CreateDbContext();
        var model = dbContext.Model;
        var workVideos = RequireEntityType(model, typeof(WorkVideo));
        var uploadSessions = RequireEntityType(model, typeof(WorkVideoUploadSession));

        AssertHasUniqueIndex(workVideos, nameof(WorkVideo.WorkId), nameof(WorkVideo.SortOrder));
        AssertRequiredCascadeForeignKey(workVideos, nameof(WorkVideo.WorkId));
        AssertRequiredCascadeForeignKey(uploadSessions, nameof(WorkVideoUploadSession.WorkId));
        AssertHasIndex(uploadSessions, nameof(WorkVideoUploadSession.WorkId));
        AssertHasIndex(uploadSessions, nameof(WorkVideoUploadSession.ExpiresAt));
    }

    [Fact]
    public async Task SaveChanges_RefreshesSearchFieldsWhenContentChanges()
    {
        await using var dbContext = CreateDbContext();
        var blog = new Blog
        {
            Title = "Initial Blog",
            Slug = "search-refresh-blog",
            Excerpt = "Initial excerpt",
            ContentJson = "{\"markdown\":\"Initial body\"}"
        };
        var work = new Work
        {
            Title = "Initial Work",
            Slug = "search-refresh-work",
            Excerpt = "Initial work excerpt",
            Category = "case-study",
            ContentJson = "{\"html\":\"<p>Initial work body</p>\"}",
            AllPropertiesJson = "{}"
        };
        dbContext.Blogs.Add(blog);
        dbContext.Works.Add(work);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        blog.Title = "Updated Blog Search";
        blog.Excerpt = "Updated blog excerpt";
        blog.ContentJson = "{\"markdown\":\"Updated blog body\"}";
        work.Title = "Updated Work Search";
        work.Excerpt = "Updated work excerpt";
        work.ContentJson = "{\"html\":\"<p>Updated work body</p>\"}";
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal("updatedblogsearch", blog.SearchTitle);
        Assert.Contains("updatedblogexcerpt", blog.SearchText, StringComparison.Ordinal);
        Assert.Contains("updatedblogbody", blog.SearchText, StringComparison.Ordinal);
        Assert.Equal("updatedworksearch", work.SearchTitle);
        Assert.Contains("updatedworkexcerpt", work.SearchText, StringComparison.Ordinal);
        Assert.Contains("updatedworkbody", work.SearchText, StringComparison.Ordinal);
    }

    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static IEntityType RequireEntityType(IModel model, Type entityType)
    {
        var resolvedEntityType = model.FindEntityType(entityType);
        return Assert.IsAssignableFrom<IEntityType>(resolvedEntityType);
    }

    private static void AssertPropertyRequired(IEntityType entityType, string propertyName)
    {
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.False(property!.IsNullable);
    }

    private static string? GetColumnType(IEntityType entityType, string propertyName)
    {
        return entityType.FindProperty(propertyName)?.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value as string;
    }

    private static void AssertHasIndex(IEntityType entityType, params string[] propertyNames)
    {
        Assert.Contains(entityType.GetIndexes(), index => HasProperties(index, propertyNames));
    }

    private static void AssertHasUniqueIndex(IEntityType entityType, params string[] propertyNames)
    {
        Assert.Contains(entityType.GetIndexes(), index => index.IsUnique && HasProperties(index, propertyNames));
    }

    private static bool HasProperties(IIndex index, string[] propertyNames)
    {
        return index.Properties.Select(property => property.Name).SequenceEqual(propertyNames);
    }

    private static void AssertRequiredCascadeForeignKey(IEntityType entityType, string propertyName)
    {
        var foreignKey = entityType.GetForeignKeys()
            .SingleOrDefault(candidate => candidate.Properties.Select(property => property.Name).SequenceEqual([propertyName]));

        Assert.NotNull(foreignKey);
        Assert.True(foreignKey!.IsRequired);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }
}
