using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Seeding;

public static class SeedData
{
    public static async Task InitializeAsync(WoongBlogDbContext dbContext)
    {
        if (await dbContext.SiteSettings.AnyAsync())
        {
            await EnsurePublicDetailSeedsAsync(dbContext);
            return;
        }

        await SeedInitialDataAsync(dbContext);
    }

    private static async Task SeedInitialDataAsync(WoongBlogDbContext dbContext)
    {
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var resumeAssetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var workThumb1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var workThumb2 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var workIcon1 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var blogCover1 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var blogCover2 = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var seededWorkVideo1 = Guid.Parse("12121212-1212-1212-1212-121212121212");
        var seededWorkVideo2 = Guid.Parse("34343434-3434-3434-3434-343434343434");

        dbContext.Assets.AddRange(
            new Asset
            {
                Id = resumeAssetId,
                Bucket = "media",
                Path = "resume/woonggon-kim-resume.pdf",
                PublicUrl = "/media/resume/woonggon-kim-resume.pdf",
                MimeType = "application/pdf",
                Kind = "pdf",
                Size = 182_432,
                CreatedBy = adminId
            },
            new Asset
            {
                Id = workThumb1,
                Bucket = "media",
                Path = "works/seeded-work-thumb.png",
                PublicUrl = "/media/works/seeded-work-thumb.png",
                MimeType = "image/png",
                Kind = "image",
                Size = 48_000,
                CreatedBy = adminId
            },
            new Asset
            {
                Id = workThumb2,
                Bucket = "media",
                Path = "works/platform-rebuild-thumb.png",
                PublicUrl = "/media/works/platform-rebuild-thumb.png",
                MimeType = "image/png",
                Kind = "image",
                Size = 52_000,
                CreatedBy = adminId
            },
            new Asset
            {
                Id = workIcon1,
                Bucket = "media",
                Path = "works/seeded-work-icon.png",
                PublicUrl = "/media/works/seeded-work-icon.png",
                MimeType = "image/png",
                Kind = "image",
                Size = 8_000,
                CreatedBy = adminId
            },
            new Asset
            {
                Id = blogCover1,
                Bucket = "media",
                Path = "blogs/seeded-blog-cover.png",
                PublicUrl = "/media/blogs/seeded-blog-cover.png",
                MimeType = "image/png",
                Kind = "image",
                Size = 31_000,
                CreatedBy = adminId
            },
            new Asset
            {
                Id = blogCover2,
                Bucket = "media",
                Path = "blogs/engineering-notes-cover.png",
                PublicUrl = "/media/blogs/engineering-notes-cover.png",
                MimeType = "image/png",
                Kind = "image",
                Size = 29_000,
                CreatedBy = adminId
            }
        );

        dbContext.SiteSettings.Add(new SiteSetting
        {
            Singleton = true,
            OwnerName = "Woonggon Kim",
            Tagline = "Creative Technologist",
            GitHubUrl = "https://github.com/woong",
            LinkedInUrl = "https://linkedin.com/in/woong",
            ResumeAssetId = resumeAssetId
        });

        dbContext.Profiles.AddRange(
            new Profile
            {
                Id = adminId,
                Email = "admin@example.com",
                DisplayName = "Admin User",
                Provider = "google",
                ProviderSubject = "seed-admin-subject",
                Role = "admin"
            },
            new Profile
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "user@example.com",
                DisplayName = "Seed User",
                Provider = "google",
                ProviderSubject = "seed-user-subject",
                Role = "user"
            }
        );

        dbContext.Pages.AddRange(
            new PageEntity
            {
                Slug = "home",
                Title = "Home",
                ContentJson = """{"headline":"Hi, I am Woonggon","introText":"I build products across frontend, backend, AI tooling, and developer workflow systems with a bias for pragmatic delivery."}"""
            },
            new PageEntity
            {
                Slug = "introduction",
                Title = "Introduction",
                ContentJson = """{"html":"<p>I work across product engineering, architecture, and delivery systems. My focus is to turn vague product ideas into stable and maintainable software.</p><p>This seeded introduction exists so the frontend can immediately render against PostgreSQL-backed content.</p>"}"""
            },
            new PageEntity
            {
                Slug = "contact",
                Title = "Contact",
                ContentJson = """{"html":"<p>You can reach me at <a href='mailto:woong@example.com'>woong@example.com</a> or through GitHub and LinkedIn.</p>"}"""
            }
        );

        var seededWork = new Work
        {
            Slug = "seeded-work",
            Title = "Portfolio Platform Rebuild",
            Excerpt = "Rebuilt the portfolio stack around clearer domain boundaries, richer content modeling, and operational simplicity.",
            ContentJson = """{"html":"<h2>Overview</h2><p>This seeded case study represents a platform rebuild that spans frontend UX, backend APIs, and deployment ergonomics.</p><h2>Highlights</h2><ul><li>React + TypeScript frontend</li><li>ASP.NET Core backend</li><li>PostgreSQL domain model</li></ul>"}""",
            ThumbnailAssetId = workThumb1,
            IconAssetId = workIcon1,
            Category = "platform",
            Period = "2025.12 - 2026.03",
            AllPropertiesJson = """{"teamSize":1,"role":"full-stack","status":"seeded"}""",
            Tags = new[] { "react", "nextjs", "dotnet", "postgres" },
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-7)
        };
        var internalAdminWorkbench = new Work
        {
            Slug = "internal-admin-workbench",
            Title = "Internal Admin Workbench",
            Excerpt = "Designed a cleaner admin workflow with shared editor chrome, preview-first ergonomics, and better information architecture.",
            ContentJson = """{"html":"<h2>Problem</h2><p>The admin experience felt like a separate product with weak draft preview.</p><h2>Result</h2><p>The workbench concept unified list, edit, preview, and publishing workflows.</p>"}""",
            ThumbnailAssetId = workThumb2,
            Category = "admin",
            Period = "2026.01 - 2026.03",
            AllPropertiesJson = """{"teamSize":1,"role":"ux-engineering","status":"seeded"}""",
            Tags = new[] { "admin", "ux", "workflow" },
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-3)
        };
        dbContext.Works.AddRange(seededWork, internalAdminWorkbench);

        dbContext.WorkVideos.AddRange(
            new WorkVideo
            {
                Id = seededWorkVideo1,
                WorkId = seededWork.Id,
                SourceType = "youtube",
                SourceKey = "dQw4w9WgXcQ",
                OriginalFileName = "Seed Overview",
                SortOrder = 0
            },
            new WorkVideo
            {
                Id = seededWorkVideo2,
                WorkId = seededWork.Id,
                SourceType = "youtube",
                SourceKey = "M7lc1UVf-VE",
                OriginalFileName = "Seed Demo",
                SortOrder = 1
            }
        );

        dbContext.Blogs.AddRange(
            new Blog
            {
                Slug = "seeded-blog",
                Title = "Designing a Seed-First Migration Strategy",
                Excerpt = "Why seed data is often the fastest way to stabilize a new architecture before historical migration work is complete.",
                ContentJson = """{"html":"<h2>Why Start With Seed Data</h2><p>Seed data gives frontend and backend teams something concrete to build against from day one.</p><h3>Reduce Coordination Cost</h3><p>That reduces ambiguity and improves testability.</p><h2>What To Stabilize First</h2><p>Lock the happy path before historical backfill.</p>"}""",
                CoverAssetId = blogCover1,
                Tags = new[] { "seed", "migration", "architecture" },
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-5)
            },
            new Blog
            {
                Slug = "engineering-notes-on-bff-auth",
                Title = "Engineering Notes on BFF-Style Auth",
                Excerpt = "Keeping authentication in the backend simplifies session handling and reduces token sprawl in the browser.",
                ContentJson = """{"html":"<h2>Why BFF Works</h2><p>BFF auth centralizes session ownership in the backend and keeps the browser thinner.</p>"}""",
                CoverAssetId = blogCover2,
                Tags = new[] { "auth", "bff", "security" },
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        );

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsurePublicDetailSeedsAsync(WoongBlogDbContext dbContext)
    {
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var workThumb1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var workIcon1 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var blogCover1 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var seededWorkVideo1 = Guid.Parse("12121212-1212-1212-1212-121212121212");
        var seededWorkVideo2 = Guid.Parse("34343434-3434-3434-3434-343434343434");

        await EnsureAssetAsync(
            dbContext,
            workThumb1,
            adminId,
            "media",
            "works/seeded-work-thumb.png",
            "/media/works/seeded-work-thumb.png",
            "image/png",
            "image",
            48_000);

        await EnsureAssetAsync(
            dbContext,
            workIcon1,
            adminId,
            "media",
            "works/seeded-work-icon.png",
            "/media/works/seeded-work-icon.png",
            "image/png",
            "image",
            8_000);

        await EnsureAssetAsync(
            dbContext,
            blogCover1,
            adminId,
            "media",
            "blogs/seeded-blog-cover.png",
            "/media/blogs/seeded-blog-cover.png",
            "image/png",
            "image",
            31_000);

        var seededWork = await dbContext.Works.SingleOrDefaultAsync(x => x.Slug == "seeded-work");
        if (seededWork is null)
        {
            seededWork = await dbContext.Works
                .OrderByDescending(x => x.PublishedAt)
                .FirstOrDefaultAsync(x => x.Title == "Portfolio Platform Rebuild");
        }

        if (seededWork is null)
        {
            seededWork = new Work
            {
                Slug = "seeded-work",
                Title = "Portfolio Platform Rebuild",
                Excerpt = "Rebuilt the portfolio stack around clearer domain boundaries, richer content modeling, and operational simplicity.",
                ContentJson = """{"html":"<h2>Overview</h2><p>This seeded case study represents a platform rebuild that spans frontend UX, backend APIs, and deployment ergonomics.</p><h2>Highlights</h2><ul><li>React + TypeScript frontend</li><li>ASP.NET Core backend</li><li>PostgreSQL domain model</li></ul>"}""",
                ThumbnailAssetId = workThumb1,
                IconAssetId = workIcon1,
                Category = "platform",
                Period = "2025.12 - 2026.03",
                AllPropertiesJson = """{"teamSize":1,"role":"full-stack","status":"seeded"}""",
                Tags = new[] { "react", "nextjs", "dotnet", "postgres" },
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-7)
            };
            dbContext.Works.Add(seededWork);
        }
        else
        {
            seededWork.Slug = "seeded-work";
            seededWork.Title = "Portfolio Platform Rebuild";
            seededWork.Excerpt = "Rebuilt the portfolio stack around clearer domain boundaries, richer content modeling, and operational simplicity.";
            seededWork.ContentJson = """{"html":"<h2>Overview</h2><p>This seeded case study represents a platform rebuild that spans frontend UX, backend APIs, and deployment ergonomics.</p><h2>Highlights</h2><ul><li>React + TypeScript frontend</li><li>ASP.NET Core backend</li><li>PostgreSQL domain model</li></ul>"}""";
            seededWork.ThumbnailAssetId = workThumb1;
            seededWork.IconAssetId = workIcon1;
            seededWork.Category = "platform";
            seededWork.Period = "2025.12 - 2026.03";
            seededWork.AllPropertiesJson = """{"teamSize":1,"role":"full-stack","status":"seeded"}""";
            seededWork.Tags = new[] { "react", "nextjs", "dotnet", "postgres" };
            seededWork.Published = true;
            seededWork.PublishedAt ??= DateTimeOffset.UtcNow.AddDays(-7);
            seededWork.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await EnsureSeededWorkVideoAsync(
            dbContext,
            seededWork.Id,
            seededWorkVideo1,
            "youtube",
            "dQw4w9WgXcQ",
            "Seed Overview",
            0);
        await EnsureSeededWorkVideoAsync(
            dbContext,
            seededWork.Id,
            seededWorkVideo2,
            "youtube",
            "M7lc1UVf-VE",
            "Seed Demo",
            1);

        var seededBlog = await dbContext.Blogs.SingleOrDefaultAsync(x => x.Slug == "seeded-blog");
        if (seededBlog is null)
        {
            seededBlog = await dbContext.Blogs
                .OrderByDescending(x => x.PublishedAt)
                .FirstOrDefaultAsync(x => x.Title == "Designing a Seed-First Migration Strategy");
        }

        if (seededBlog is null)
        {
            seededBlog = new Blog
            {
                Slug = "seeded-blog",
                Title = "Designing a Seed-First Migration Strategy",
                Excerpt = "Why seed data is often the fastest way to stabilize a new architecture before historical migration work is complete.",
                ContentJson = """{"html":"<h2>Why Start With Seed Data</h2><p>Seed data gives frontend and backend teams something concrete to build against from day one.</p><h3>Reduce Coordination Cost</h3><p>That reduces ambiguity and improves testability.</p><h2>What To Stabilize First</h2><p>Lock the happy path before historical backfill.</p>"}""",
                CoverAssetId = blogCover1,
                Tags = new[] { "seed", "migration", "architecture" },
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-5)
            };
            dbContext.Blogs.Add(seededBlog);
        }
        else
        {
            seededBlog.Slug = "seeded-blog";
            seededBlog.Title = "Designing a Seed-First Migration Strategy";
            seededBlog.Excerpt = "Why seed data is often the fastest way to stabilize a new architecture before historical migration work is complete.";
            seededBlog.ContentJson = """{"html":"<h2>Why Start With Seed Data</h2><p>Seed data gives frontend and backend teams something concrete to build against from day one.</p><h3>Reduce Coordination Cost</h3><p>That reduces ambiguity and improves testability.</p><h2>What To Stabilize First</h2><p>Lock the happy path before historical backfill.</p>"}""";
            seededBlog.CoverAssetId = blogCover1;
            seededBlog.Tags = new[] { "seed", "migration", "architecture" };
            seededBlog.Published = true;
            seededBlog.PublishedAt ??= DateTimeOffset.UtcNow.AddDays(-5);
            seededBlog.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureSeededWorkVideoAsync(
        WoongBlogDbContext dbContext,
        Guid workId,
        Guid videoId,
        string sourceType,
        string sourceKey,
        string originalFileName,
        int sortOrder)
    {
        var existingVideo = dbContext.WorkVideos.Local.FirstOrDefault(x => x.WorkId == workId && x.SortOrder == sortOrder)
            ?? dbContext.WorkVideos.Local.FirstOrDefault(x => x.Id == videoId)
            ?? await dbContext.WorkVideos.SingleOrDefaultAsync(x => x.WorkId == workId && x.SortOrder == sortOrder)
            ?? await dbContext.WorkVideos.SingleOrDefaultAsync(x => x.Id == videoId);
        if (existingVideo is null)
        {
            dbContext.WorkVideos.Add(new WorkVideo
            {
                Id = videoId,
                WorkId = workId,
                SourceType = sourceType,
                SourceKey = sourceKey,
                OriginalFileName = originalFileName,
                SortOrder = sortOrder
            });

            return;
        }

        existingVideo.WorkId = workId;
        existingVideo.SourceType = sourceType;
        existingVideo.SourceKey = sourceKey;
        existingVideo.OriginalFileName = originalFileName;
        existingVideo.SortOrder = sortOrder;
    }

    private static async Task EnsureAssetAsync(
        WoongBlogDbContext dbContext,
        Guid id,
        Guid createdBy,
        string bucket,
        string path,
        string publicUrl,
        string mimeType,
        string kind,
        long size)
    {
        if (await dbContext.Assets.AnyAsync(x => x.Id == id))
        {
            return;
        }

        dbContext.Assets.Add(new Asset
        {
            Id = id,
            Bucket = bucket,
            Path = path,
            PublicUrl = publicUrl,
            MimeType = mimeType,
            Kind = kind,
            Size = size,
            CreatedBy = createdBy
        });
    }
}
