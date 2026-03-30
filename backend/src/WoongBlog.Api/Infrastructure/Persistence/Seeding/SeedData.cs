using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Seeding;

public static class SeedData
{
    public static async Task InitializeAsync(WoongBlogDbContext dbContext)
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
            Asset.Create("media", "resume/woonggon-kim-resume.pdf", "/media/resume/woonggon-kim-resume.pdf", "application/pdf", "pdf", 182_432, adminId, DateTimeOffset.UtcNow, resumeAssetId),
            Asset.Create("media", "works/seeded-work-thumb.png", "/media/works/seeded-work-thumb.png", "image/png", "image", 48_000, adminId, DateTimeOffset.UtcNow, workThumb1),
            Asset.Create("media", "works/platform-rebuild-thumb.png", "/media/works/platform-rebuild-thumb.png", "image/png", "image", 52_000, adminId, DateTimeOffset.UtcNow, workThumb2),
            Asset.Create("media", "works/seeded-work-icon.png", "/media/works/seeded-work-icon.png", "image/png", "image", 8_000, adminId, DateTimeOffset.UtcNow, workIcon1),
            Asset.Create("media", "blogs/seeded-blog-cover.png", "/media/blogs/seeded-blog-cover.png", "image/png", "image", 31_000, adminId, DateTimeOffset.UtcNow, blogCover1),
            Asset.Create("media", "blogs/engineering-notes-cover.png", "/media/blogs/engineering-notes-cover.png", "image/png", "image", 29_000, adminId, DateTimeOffset.UtcNow, blogCover2)
        );

        dbContext.SiteSettings.Add(SiteSetting.Create(
            "Woonggon Kim",
            "Creative Technologist",
            string.Empty,
            string.Empty,
            string.Empty,
            "https://linkedin.com/in/woong",
            "https://github.com/woong",
            resumeAssetId,
            DateTimeOffset.UtcNow));

        dbContext.Profiles.AddRange(
            Profile.Seed(adminId, "admin@example.com", "Admin User", "google", "seed-admin-subject", "admin"),
            Profile.Seed(Guid.Parse("22222222-2222-2222-2222-222222222222"), "user@example.com", "Seed User", "google", "seed-user-subject", "user")
        );

        dbContext.Pages.AddRange(
            PageEntity.Create("home", "Home", """{"headline":"Hi, I am Woonggon","introText":"I build products across frontend, backend, AI tooling, and developer workflow systems with a bias for pragmatic delivery."}"""),
            PageEntity.Create("introduction", "Introduction", """{"html":"<p>I work across product engineering, architecture, and delivery systems. My focus is to turn vague product ideas into stable and maintainable software.</p><p>This seeded introduction exists so the frontend can immediately render against PostgreSQL-backed content.</p>"}"""),
            PageEntity.Create("contact", "Contact", """{"html":"<p>You can reach me at <a href='mailto:woong@example.com'>woong@example.com</a> or through GitHub and LinkedIn.</p>"}""")
        );

        dbContext.Works.AddRange(
            Work.Seed(new WorkUpsertValues("Portfolio Platform Rebuild", "platform", "2025.12 - 2026.03", new[] { "react", "nextjs", "dotnet", "postgres" }, true, """{"html":"<h2>Overview</h2><p>This seeded case study represents a platform rebuild that spans frontend UX, backend APIs, and deployment ergonomics.</p><h2>Highlights</h2><ul><li>React + TypeScript frontend</li><li>ASP.NET Core backend</li><li>PostgreSQL domain model</li></ul>"}""", """{"teamSize":1,"role":"full-stack","status":"seeded"}""", workThumb1, workIcon1), "seeded-work", "Rebuilt the portfolio stack around clearer domain boundaries, richer content modeling, and operational simplicity.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddDays(-7)),
            Work.Seed(new WorkUpsertValues("Internal Admin Workbench", "admin", "2026.01 - 2026.03", new[] { "admin", "ux", "workflow" }, true, """{"html":"<h2>Problem</h2><p>The admin experience felt like a separate product with weak draft preview.</p><h2>Result</h2><p>The workbench concept unified list, edit, preview, and publishing workflows.</p>"}""", """{"teamSize":1,"role":"ux-engineering","status":"seeded"}""", workThumb2, null), "internal-admin-workbench", "Designed a cleaner admin workflow with shared editor chrome, preview-first ergonomics, and better information architecture.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddDays(-3))
        );

        for (var index = 1; index <= 11; index += 1)
        {
            dbContext.Works.Add(Work.Seed(
                new WorkUpsertValues(
                    $"Seeded Work {index}",
                    index % 2 == 0 ? "platform" : "admin",
                    $"2026.0{((index - 1) % 6) + 1} - 2026.0{((index - 1) % 6) + 2}",
                    new[] { "seeded", "work", $"batch-{index}" },
                    true,
                    $$"""{"html":"<h2>Seeded Work {{index}}</h2><p>This seeded work entry exists to support public grid, pagination, and related content tests.</p>"}""",
                    $$"""{"teamSize":1,"role":"seeded","index":{{index}}}""",
                    index % 2 == 0 ? workThumb1 : workThumb2,
                    workIcon1),
                $"seeded-work-{index}",
                $"Seeded work excerpt {index} for pagination, related content, and layout stability coverage.",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                DateTimeOffset.UtcNow.AddDays(-(10 + index))));
        }

        dbContext.Blogs.AddRange(
            Blog.Seed(new BlogUpsertValues("Designing a Seed-First Migration Strategy", new[] { "seed", "migration", "architecture" }, true, """{"html":"<p>Seed data gives frontend and backend teams something concrete to build against from day one.</p><p>That reduces ambiguity and improves testability.</p>"}""", blogCover1), "seeded-blog", "Why seed data is often the fastest way to stabilize a new architecture before historical migration work is complete.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddDays(-5)),
            Blog.Seed(new BlogUpsertValues("Engineering Notes on BFF-Style Auth", new[] { "auth", "bff", "security" }, true, """{"html":"<p>BFF auth centralizes session ownership in the backend and keeps the browser thinner.</p>"}""", blogCover2), "engineering-notes-on-bff-auth", "Keeping authentication in the backend simplifies session handling and reduces token sprawl in the browser.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddDays(-1))
        );

        for (var index = 1; index <= 22; index += 1)
        {
            dbContext.Blogs.Add(Blog.Seed(
                new BlogUpsertValues(
                    $"Seeded Blog {index}",
                    new[] { "seeded", "blog", $"batch-{index}" },
                    true,
                    $$"""{"html":"<p>Seeded blog {{index}} body exists to support public pagination, blog detail rendering, and related content coverage.</p>"}""",
                    index % 2 == 0 ? blogCover1 : blogCover2),
                $"seeded-blog-{index}",
                $"Seeded blog excerpt {index} for public pagination, detail, and related-content stability tests.",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                DateTimeOffset.UtcNow.AddDays(-(6 + index))));
        }

        await dbContext.SaveChangesAsync();
    }
}
