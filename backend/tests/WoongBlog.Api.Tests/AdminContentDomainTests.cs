using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Tests;

public class AdminContentDomainTests
{
    [Fact]
    public void Work_Create_SetsExcerpt_Timestamps_AndPublishedAt()
    {
        var now = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero);
        var work = Work.Create(
            new WorkUpsertValues("Title", "platform", "2026.03", ["tag"], true, """{"html":"<p>Hello</p>"}""", "{}", null, null),
            "title",
            "Hello",
            now);

        Assert.Equal("Hello", work.Excerpt);
        Assert.Equal(now, work.CreatedAt);
        Assert.Equal(now, work.UpdatedAt);
        Assert.Equal(now, work.PublishedAt);
    }

    [Fact]
    public void Work_Update_SetsPublishedAt_OnlyOnFirstPublish()
    {
        var createdAt = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var work = Work.Create(
            new WorkUpsertValues("Title", "platform", "2026.03", [], false, """{"html":"<p>Hello</p>"}""", "{}", null, null),
            "title",
            "Hello",
            createdAt);
        var publishedAt = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

        work.Update(
            new WorkUpsertValues("Title", "platform", "2026.03", [], true, """{"html":"<p>Hello</p>"}""", "{}", null, null),
            "title",
            "Hello",
            publishedAt);
        var firstPublishedAt = work.PublishedAt;

        work.Update(
            new WorkUpsertValues("Title", "platform", "2026.03", [], false, """{"html":"<p>Hello again</p>"}""", "{}", null, null),
            "title",
            "Hello again",
            publishedAt.AddDays(1));

        Assert.Equal(firstPublishedAt, work.PublishedAt);
        Assert.False(work.Published);
    }

    [Fact]
    public void PageEntity_UpdateContent_UpdatesFields()
    {
        var page = PageEntity.Create("page", "Old", "{}");
        var now = new DateTimeOffset(2026, 3, 30, 1, 0, 0, TimeSpan.Zero);

        page.UpdateContent("New", """{"html":"<p>Updated</p>"}""", now);

        Assert.Equal("New", page.Title);
        Assert.Equal("""{"html":"<p>Updated</p>"}""", page.ContentJson);
        Assert.Equal(now, page.UpdatedAt);
    }

    [Fact]
    public async Task AdminUniqueSlugService_Appends_Suffix_For_Duplicates()
    {
        var workStore = new FakeWorkWriteStore(["sample-work", "sample-work-2"]);
        var service = new AdminUniqueSlugService(workStore, new FakeBlogWriteStore([]));

        var slug = await service.GenerateWorkSlugAsync("Sample Work", null, CancellationToken.None);

        Assert.Equal("sample-work-3", slug);
    }

    [Fact]
    public void AdminExcerptService_Generates_Work_And_Blog_Excerpts()
    {
        var service = new AdminExcerptService();

        var workExcerpt = service.GenerateWorkExcerpt("""{"html":"<p>Hello <strong>work</strong></p>"}""");
        var blogExcerpt = service.GenerateBlogExcerpt("""{"markdown":"# Heading\nBody text"}""");

        Assert.Equal("Hello work", workExcerpt);
        Assert.Equal("Heading Body text", blogExcerpt);
    }

    private sealed class FakeWorkWriteStore : IAdminWorkWriteStore
    {
        private readonly HashSet<string> _slugs;

        public FakeWorkWriteStore(IEnumerable<string> slugs)
        {
            _slugs = new HashSet<string>(slugs, StringComparer.Ordinal);
        }

        public Task<Work?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Work?>(null);
        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(_slugs.Contains(slug));
        public void Add(Work work) { }
        public void Remove(Work work) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBlogWriteStore : IAdminBlogWriteStore
    {
        private readonly HashSet<string> _slugs;

        public FakeBlogWriteStore(IEnumerable<string> slugs)
        {
            _slugs = new HashSet<string>(slugs, StringComparer.Ordinal);
        }

        public Task<Blog?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Blog?>(null);
        public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(_slugs.Contains(slug));
        public void Add(Blog blog) { }
        public void Remove(Blog blog) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
