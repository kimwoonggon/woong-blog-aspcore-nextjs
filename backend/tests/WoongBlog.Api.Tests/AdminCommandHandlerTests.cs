using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.CreateBlog;
using WoongBlog.Api.Application.Admin.CreateWork;
using WoongBlog.Api.Application.Admin.GetAdminMembers;
using WoongBlog.Api.Application.Admin.GetDashboardSummary;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Admin.UpdateBlog;
using WoongBlog.Api.Application.Admin.UpdatePage;
using WoongBlog.Api.Application.Admin.UpdateSiteSettings;
using WoongBlog.Api.Application.Admin.UpdateWork;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public class AdminCommandHandlerTests
{
    [Fact]
    public async Task CreateWorkHandler_CreatesAndSaves_Work()
    {
        var store = new FakeWorkWriteStore();
        var handler = new CreateWorkCommandHandler(store, new FakeSlugService(workSlug: "work-slug"), new FakeExcerptService(workExcerpt: "work-excerpt"));

        var result = await handler.Handle(new CreateWorkCommand("Title", "platform", "2026.03", ["a"], true, """{"html":"<p>Body</p>"}""", "{}", null, null), CancellationToken.None);

        Assert.Equal("work-slug", result.Slug);
        Assert.True(store.SaveCalled);
        Assert.NotNull(store.AddedWork);
        Assert.Equal("work-excerpt", store.AddedWork!.Excerpt);
    }

    [Fact]
    public async Task UpdateWorkHandler_ReturnsNull_WhenMissing()
    {
        var handler = new UpdateWorkCommandHandler(new FakeWorkWriteStore(), new FakeSlugService(), new FakeExcerptService());

        var result = await handler.Handle(new UpdateWorkCommand(Guid.NewGuid(), "Title", "platform", "2026.03", [], false, "{}", "{}", null, null), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateWorkHandler_UpdatesExistingWork_AndSaves()
    {
        var existing = Work.Seed(new WorkUpsertValues("Old", "platform", "2026.01", [], false, "{}", "{}", null, null), "old", "old", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid());
        var store = new FakeWorkWriteStore(existing);
        var handler = new UpdateWorkCommandHandler(store, new FakeSlugService(workSlug: "new-slug"), new FakeExcerptService(workExcerpt: "new-excerpt"));

        var result = await handler.Handle(new UpdateWorkCommand(existing.Id, "New", "platform", "2026.03", ["tag"], true, """{"html":"<p>Body</p>"}""", "{}", null, null), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new-slug", existing.Slug);
        Assert.Equal("new-excerpt", existing.Excerpt);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    public async Task CreateBlogHandler_CreatesAndSaves_Blog()
    {
        var store = new FakeBlogWriteStore();
        var handler = new CreateBlogCommandHandler(store, new FakeSlugService(blogSlug: "blog-slug"), new FakeExcerptService(blogExcerpt: "blog-excerpt"));

        var result = await handler.Handle(new CreateBlogCommand("Title", ["tag"], true, """{"html":"<p>Body</p>"}"""), CancellationToken.None);

        Assert.Equal("blog-slug", result.Slug);
        Assert.Equal("blog-excerpt", store.AddedBlog!.Excerpt);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    public async Task UpdateBlogHandler_ReturnsNull_WhenMissing()
    {
        var handler = new UpdateBlogCommandHandler(new FakeBlogWriteStore(), new FakeSlugService(), new FakeExcerptService());

        var result = await handler.Handle(new UpdateBlogCommand(Guid.NewGuid(), "Title", [], false, "{}"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateBlogHandler_UpdatesExistingBlog()
    {
        var existing = Blog.Seed(new BlogUpsertValues("Old", [], false, "{}", null), "old", "old", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid());
        var store = new FakeBlogWriteStore(existing);
        var handler = new UpdateBlogCommandHandler(store, new FakeSlugService(blogSlug: "new-blog"), new FakeExcerptService(blogExcerpt: "new-blog-excerpt"));

        var result = await handler.Handle(new UpdateBlogCommand(existing.Id, "New", ["tag"], true, """{"html":"<p>Body</p>"}"""), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new-blog", existing.Slug);
        Assert.Equal("new-blog-excerpt", existing.Excerpt);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    public async Task UpdatePageHandler_ReturnsFalse_WhenPageMissing()
    {
        var handler = new UpdatePageCommandHandler(new FakePageWriteStore());

        var result = await handler.Handle(new UpdatePageCommand(Guid.NewGuid(), "Title", "{}"), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task UpdatePageHandler_UpdatesExistingPage()
    {
        var page = PageEntity.Create("page", "Old", "{}");
        var store = new FakePageWriteStore(page);
        var handler = new UpdatePageCommandHandler(store);

        var result = await handler.Handle(new UpdatePageCommand(page.Id, "New", """{"html":"<p>Updated</p>"}"""), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("New", page.Title);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    public async Task UpdateSiteSettingsHandler_ReturnsFalse_WhenMissing()
    {
        var handler = new UpdateSiteSettingsCommandHandler(new FakeSiteSettingsWriteStore());

        var result = await handler.Handle(new UpdateSiteSettingsCommand("Owner", null, null, null, null, null, null, null, false), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task UpdateSiteSettingsHandler_UpdatesExistingSettings()
    {
        var settings = SiteSetting.Create("Owner", "Tagline", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, DateTimeOffset.UtcNow);
        var store = new FakeSiteSettingsWriteStore(settings);
        var handler = new UpdateSiteSettingsCommandHandler(store);

        var result = await handler.Handle(new UpdateSiteSettingsCommand("New Owner", "New Tagline", null, null, null, null, null, Guid.NewGuid(), true), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("New Owner", settings.OwnerName);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    public async Task GetAdminMembersQueryHandler_DelegatesToQueries()
    {
        var handler = new GetAdminMembersQueryHandler(new FakeAdminMemberQueries([new AdminMemberListItemDto(Guid.NewGuid(), "Name", "email@example.com", "admin", "google", DateTimeOffset.UtcNow, null, 1)]));

        var result = await handler.Handle(new GetAdminMembersQuery(), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetDashboardSummaryQueryHandler_DelegatesToQueries()
    {
        var handler = new GetDashboardSummaryQueryHandler(new FakeAdminDashboardQueries(new AdminDashboardSummaryDto(1, 2, 3)));

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(1, result.WorksCount);
        Assert.Equal(2, result.BlogsCount);
        Assert.Equal(3, result.ViewsCount);
    }

    [Fact]
    public async Task AuthProfileLookupService_ReturnsProfileByEmail_OrNull()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new WoongBlogDbContext(options);
        dbContext.Profiles.Add(Profile.Seed(Guid.NewGuid(), "user@example.com", "User", "google", "subject", "user"));
        await dbContext.SaveChangesAsync();

        var service = new AuthProfileLookupService(dbContext);
        var found = await service.GetByEmailAsync("user@example.com");
        var missing = await service.GetByEmailAsync("missing@example.com");

        Assert.NotNull(found);
        Assert.Equal("user@example.com", found!.Email);
        Assert.Null(missing);
    }

    private sealed class FakeSlugService : IAdminUniqueSlugService
    {
        private readonly string _workSlug;
        private readonly string _blogSlug;

        public FakeSlugService(string workSlug = "work", string blogSlug = "blog")
        {
            _workSlug = workSlug;
            _blogSlug = blogSlug;
        }

        public Task<string> GenerateWorkSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(_workSlug);
        public Task<string> GenerateBlogSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(_blogSlug);
    }

    private sealed class FakeExcerptService : IAdminExcerptService
    {
        private readonly string _workExcerpt;
        private readonly string _blogExcerpt;

        public FakeExcerptService(string workExcerpt = "work-excerpt", string blogExcerpt = "blog-excerpt")
        {
            _workExcerpt = workExcerpt;
            _blogExcerpt = blogExcerpt;
        }

        public string GenerateWorkExcerpt(string contentJson) => _workExcerpt;
        public string GenerateBlogExcerpt(string contentJson) => _blogExcerpt;
    }

    private sealed class FakeWorkWriteStore : IAdminWorkWriteStore
    {
        private readonly Work? _existing;
        public Work? AddedWork { get; private set; }
        public bool SaveCalled { get; private set; }

        public FakeWorkWriteStore(Work? existing = null)
        {
            _existing = existing;
        }

        public Task<Work?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_existing?.Id == id ? _existing : null);
        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(Work work) => AddedWork = work;
        public void Remove(Work work) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakeBlogWriteStore : IAdminBlogWriteStore
    {
        private readonly Blog? _existing;
        public Blog? AddedBlog { get; private set; }
        public bool SaveCalled { get; private set; }

        public FakeBlogWriteStore(Blog? existing = null)
        {
            _existing = existing;
        }

        public Task<Blog?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_existing?.Id == id ? _existing : null);
        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(Blog blog) => AddedBlog = blog;
        public void Remove(Blog blog) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakePageWriteStore : IAdminPageWriteStore
    {
        private readonly PageEntity? _page;
        public bool SaveCalled { get; private set; }

        public FakePageWriteStore(PageEntity? page = null)
        {
            _page = page;
        }

        public Task<PageEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_page?.Id == id ? _page : null);
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakeSiteSettingsWriteStore : IAdminSiteSettingsWriteStore
    {
        private readonly SiteSetting? _settings;
        public bool SaveCalled { get; private set; }

        public FakeSiteSettingsWriteStore(SiteSetting? settings = null)
        {
            _settings = settings;
        }

        public Task<SiteSetting?> GetSingletonAsync(CancellationToken cancellationToken) => Task.FromResult(_settings);
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakeAdminMemberQueries : IAdminMemberQueries
    {
        private readonly IReadOnlyList<AdminMemberListItemDto> _items;

        public FakeAdminMemberQueries(IReadOnlyList<AdminMemberListItemDto> items)
        {
            _items = items;
        }

        public Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(_items);
    }

    private sealed class FakeAdminDashboardQueries : IAdminDashboardQueries
    {
        private readonly AdminDashboardSummaryDto _summary;

        public FakeAdminDashboardQueries(AdminDashboardSummaryDto summary)
        {
            _summary = summary;
        }

        public Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken) => Task.FromResult(_summary);
    }
}
