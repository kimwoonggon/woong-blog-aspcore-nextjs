using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public sealed class PostgresPersistenceContractTests : IClassFixture<PostgresPersistenceContractTests.PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresPersistenceContractTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Bootstrapper_AppliesPostgresSpecificSearchSchema()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Blogs' AND column_name = 'SearchTitle')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'SearchText')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Blogs_SearchTitle_Trgm')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Works_SearchText_Trgm')",
            cancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260419_content_search_fields')",
            cancellationToken));
    }

    [Fact]
    public async Task Bootstrapper_IsIdempotentAndPreservesRuntimeData_WithPostgres()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        await ResetDatabaseAsync(dbContext, cancellationToken);
        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var countsBeforeRuntimeData = await CountSeededDataAsync(dbContext, cancellationToken);
        dbContext.Blogs.Add(CreateBlog("runtime-preserved-blog", "Runtime Preserved Blog"));
        await dbContext.SaveChangesAsync(cancellationToken);

        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);
        await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);

        var countsAfterRepeatedBootstrap = await CountSeededDataAsync(dbContext, cancellationToken);
        Assert.Equal(countsBeforeRuntimeData.Blogs + 1, countsAfterRepeatedBootstrap.Blogs);
        Assert.Equal(countsBeforeRuntimeData.Works, countsAfterRepeatedBootstrap.Works);
        Assert.Equal(countsBeforeRuntimeData.Pages, countsAfterRepeatedBootstrap.Pages);
        Assert.Equal(countsBeforeRuntimeData.SiteSettings, countsAfterRepeatedBootstrap.SiteSettings);
        Assert.Equal(countsBeforeRuntimeData.SchemaPatches, countsAfterRepeatedBootstrap.SchemaPatches);
        Assert.Equal(1, await dbContext.Blogs.CountAsync(blog => blog.Slug == "runtime-preserved-blog", cancellationToken));
    }

    [Fact]
    public async Task RelationalConstraints_EnforceRequiredUniqueAndCascadeContracts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var dbContext = CreateDbContext())
        {
            await ResetDatabaseAsync(dbContext, cancellationToken);
            await DatabaseBootstrapper.InitializeAsync(dbContext, cancellationToken);
        }

        await using (var dbContext = CreateDbContext())
        {
            dbContext.Blogs.Add(new Blog
            {
                Slug = "required-title-blog",
                Title = null!,
                Excerpt = "required title",
                ContentJson = "{}"
            });

            await AssertPostgresConstraintViolationAsync(
                PostgresErrorCodes.NotNullViolation,
                () => dbContext.SaveChangesAsync(cancellationToken));
        }

        await using (var dbContext = CreateDbContext())
        {
            dbContext.Blogs.AddRange(
                CreateBlog("duplicate-blog-slug", "Duplicate Blog One"),
                CreateBlog("duplicate-blog-slug", "Duplicate Blog Two"));

            await AssertPostgresConstraintViolationAsync(
                PostgresErrorCodes.UniqueViolation,
                () => dbContext.SaveChangesAsync(cancellationToken));
        }

        await using (var dbContext = CreateDbContext())
        {
            dbContext.AuthSessions.AddRange(
                new AuthSession
                {
                    ProfileId = Guid.NewGuid(),
                    SessionKey = "duplicate-session-key",
                    UserAgent = "Test",
                    IpAddress = "127.0.0.1"
                },
                new AuthSession
                {
                    ProfileId = Guid.NewGuid(),
                    SessionKey = "duplicate-session-key",
                    UserAgent = "Test",
                    IpAddress = "127.0.0.1"
                });

            await AssertPostgresConstraintViolationAsync(
                PostgresErrorCodes.UniqueViolation,
                () => dbContext.SaveChangesAsync(cancellationToken));
        }

        await using (var dbContext = CreateDbContext())
        {
            var work = CreateWork("cascade-work");
            dbContext.Works.Add(work);
            dbContext.WorkVideos.AddRange(
                new WorkVideo
                {
                    WorkId = work.Id,
                    SourceType = "local",
                    SourceKey = "videos/cascade-0.mp4",
                    OriginalFileName = "Cascade 0.mp4",
                    SortOrder = 0
                },
                new WorkVideo
                {
                    WorkId = work.Id,
                    SourceType = "local",
                    SourceKey = "videos/cascade-1.mp4",
                    OriginalFileName = "Cascade 1.mp4",
                    SortOrder = 1
                });
            dbContext.WorkVideoUploadSessions.Add(new WorkVideoUploadSession
            {
                WorkId = work.Id,
                StorageType = "local",
                StorageKey = "uploads/cascade.mp4",
                OriginalFileName = "Cascade.mp4",
                ExpectedMimeType = "video/mp4",
                ExpectedSize = 1024,
                Status = WorkVideoUploadSessionStatuses.Issued,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.ChangeTracker.Clear();
            var reloadedWork = await dbContext.Works.SingleAsync(candidate => candidate.Id == work.Id, cancellationToken);
            dbContext.Works.Remove(reloadedWork);
            await dbContext.SaveChangesAsync(cancellationToken);

            Assert.False(await dbContext.WorkVideos.AnyAsync(video => video.WorkId == work.Id, cancellationToken));
            Assert.False(await dbContext.WorkVideoUploadSessions.AnyAsync(session => session.WorkId == work.Id, cancellationToken));
        }
    }

    private WoongBlogDbContext CreateDbContext()
    {
        return _fixture.CreateDbContext();
    }

    private static async Task ResetDatabaseAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private static async Task<DataCounts> CountSeededDataAsync(
        WoongBlogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return new DataCounts(
            await dbContext.Blogs.CountAsync(cancellationToken),
            await dbContext.Works.CountAsync(cancellationToken),
            await dbContext.Pages.CountAsync(cancellationToken),
            await dbContext.SiteSettings.CountAsync(cancellationToken),
            await dbContext.SchemaPatches.CountAsync(cancellationToken));
    }

    private static Blog CreateBlog(string slug, string title)
    {
        return new Blog
        {
            Slug = slug,
            Title = title,
            Excerpt = $"{title} excerpt",
            ContentJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow
        };
    }

    private static Work CreateWork(string slug)
    {
        return new Work
        {
            Slug = slug,
            Title = "Cascade Work",
            Excerpt = "Cascade work excerpt",
            Category = "case-study",
            ContentJson = "{}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task AssertPostgresConstraintViolationAsync(
        string expectedSqlState,
        Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(action);
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(expectedSqlState, postgresException.SqlState);
    }

    private static async Task<bool> ExistsAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private sealed record DataCounts(
        int Blogs,
        int Works,
        int Pages,
        int SiteSettings,
        int SchemaPatches);

    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("portfolio")
            .WithUsername("portfolio")
            .WithPassword("portfolio")
            .Build();

        public async ValueTask InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        public WoongBlogDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            return new WoongBlogDbContext(options);
        }
    }
}
