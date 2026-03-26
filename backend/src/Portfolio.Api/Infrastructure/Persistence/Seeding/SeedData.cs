using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Domain.Entities;

namespace Portfolio.Api.Infrastructure.Persistence.Seeding;

public static class SeedData
{
    public static async Task InitializeAsync(PortfolioDbContext dbContext)
    {
        if (await dbContext.SiteSettings.AnyAsync())
        {
            return;
        }

        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var resumeAssetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var workThumb1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var workThumb2 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var workIcon1 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var blogCover1 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var blogCover2 = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

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

        dbContext.Works.AddRange(
            new Work
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
            },
            new Work
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
            }
        );

        dbContext.Blogs.AddRange(
            new Blog
            {
                Slug = "seeded-blog",
                Title = "Designing a Seed-First Migration Strategy",
                Excerpt = "Why seed data is often the fastest way to stabilize a new architecture before historical migration work is complete.",
                ContentJson = """{"html":"<p>Seed data gives frontend and backend teams something concrete to build against from day one.</p><p>That reduces ambiguity and improves testability.</p>"}""",
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
                ContentJson = """{"html":"<p>BFF auth centralizes session ownership in the backend and keeps the browser thinner.</p>"}""",
                CoverAssetId = blogCover2,
                Tags = new[] { "auth", "bff", "security" },
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        );

        await dbContext.SaveChangesAsync();
    }
}
