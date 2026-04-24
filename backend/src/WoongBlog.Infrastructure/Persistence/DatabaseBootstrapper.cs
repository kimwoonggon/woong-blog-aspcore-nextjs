using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence.Seeding;

namespace WoongBlog.Infrastructure.Persistence;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Keep startup behavior centralized so the eventual migration-based path
        // can replace this in one place without changing the application entrypoint.
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                await EnsureAiBatchTablesAsync(dbContext, cancellationToken);
                await EnsureSchemaPatchesTableAsync(dbContext, cancellationToken);
                await ApplySchemaPatchAsync(
                    dbContext,
                    "20260410_work_videos",
                    WorkVideoSchemaPatchSql,
                    cancellationToken);
                await ApplySchemaPatchAsync(
                    dbContext,
                    "20260419_content_search_fields",
                    ContentSearchSchemaPatchSql,
                    cancellationToken);
                await ApplySchemaPatchAsync(
                    dbContext,
                    "20260424_work_video_metadata",
                    WorkVideoMetadataSchemaPatchSql,
                    cancellationToken);
                await SeedData.InitializeAsync(dbContext);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private static async Task EnsureAiBatchTablesAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        const string createJobsSql = """
CREATE TABLE IF NOT EXISTS "AiBatchJobs" (
  "Id" uuid PRIMARY KEY,
  "TargetType" text NOT NULL,
  "Status" text NOT NULL,
  "SelectionMode" text NOT NULL DEFAULT 'selected',
  "SelectionLabel" text NOT NULL DEFAULT '',
  "SelectionKey" text NOT NULL DEFAULT '',
  "All" boolean NOT NULL,
  "AutoApply" boolean NOT NULL DEFAULT FALSE,
  "WorkerCount" integer NULL,
  "CancelRequested" boolean NOT NULL,
  "TotalCount" integer NOT NULL,
  "ProcessedCount" integer NOT NULL,
  "SucceededCount" integer NOT NULL,
  "FailedCount" integer NOT NULL,
  "Provider" text NOT NULL,
  "Model" text NOT NULL,
  "ReasoningEffort" text NULL,
  "PromptMode" text NOT NULL,
  "CustomPrompt" text NULL,
  "RequestedByProfileId" uuid NULL,
  "CreatedAt" timestamptz NOT NULL,
  "StartedAt" timestamptz NULL,
  "FinishedAt" timestamptz NULL,
  "UpdatedAt" timestamptz NOT NULL
);
ALTER TABLE "AiBatchJobs" ADD COLUMN IF NOT EXISTS "SelectionMode" text NOT NULL DEFAULT 'selected';
ALTER TABLE "AiBatchJobs" ADD COLUMN IF NOT EXISTS "SelectionLabel" text NOT NULL DEFAULT '';
ALTER TABLE "AiBatchJobs" ADD COLUMN IF NOT EXISTS "SelectionKey" text NOT NULL DEFAULT '';
ALTER TABLE "AiBatchJobs" ADD COLUMN IF NOT EXISTS "AutoApply" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "AiBatchJobs" ADD COLUMN IF NOT EXISTS "WorkerCount" integer NULL;
ALTER TABLE "AiBatchJobs" ADD COLUMN IF NOT EXISTS "CustomPrompt" text NULL;
CREATE INDEX IF NOT EXISTS "IX_AiBatchJobs_TargetType_Status_CreatedAt" ON "AiBatchJobs" ("TargetType", "Status", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_AiBatchJobs_TargetType_SelectionKey_Status" ON "AiBatchJobs" ("TargetType", "SelectionKey", "Status");
""";

        const string createJobItemsSql = """
CREATE TABLE IF NOT EXISTS "AiBatchJobItems" (
  "Id" uuid PRIMARY KEY,
  "JobId" uuid NOT NULL,
  "EntityId" uuid NOT NULL,
  "Title" text NOT NULL,
  "Status" text NOT NULL,
  "FixedHtml" text NULL,
  "Error" text NULL,
  "Provider" text NULL,
  "Model" text NULL,
  "ReasoningEffort" text NULL,
  "CreatedAt" timestamptz NOT NULL,
  "StartedAt" timestamptz NULL,
  "FinishedAt" timestamptz NULL,
  "AppliedAt" timestamptz NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AiBatchJobItems_JobId_EntityId" ON "AiBatchJobItems" ("JobId", "EntityId");
CREATE INDEX IF NOT EXISTS "IX_AiBatchJobItems_JobId_Status" ON "AiBatchJobItems" ("JobId", "Status");
""";

        await dbContext.Database.ExecuteSqlRawAsync(createJobsSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(createJobItemsSql, cancellationToken);
    }

    private static async Task EnsureSchemaPatchesTableAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        const string sql = """
CREATE TABLE IF NOT EXISTS "SchemaPatches" (
  "Id" text PRIMARY KEY,
  "AppliedAt" timestamptz NOT NULL
);
""";

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task ApplySchemaPatchAsync(
        WoongBlogDbContext dbContext,
        string patchId,
        string sql,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        var alreadyApplied = await dbContext.SchemaPatches.AnyAsync(x => x.Id == patchId, cancellationToken);
        if (alreadyApplied)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        dbContext.SchemaPatches.Add(new SchemaPatch
        {
            Id = patchId,
            AppliedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private const string WorkVideoSchemaPatchSql = """
ALTER TABLE "Works" ADD COLUMN IF NOT EXISTS "VideosVersion" integer NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "WorkVideos" (
  "Id" uuid PRIMARY KEY,
  "WorkId" uuid NOT NULL REFERENCES "Works" ("Id") ON DELETE CASCADE,
  "SourceType" text NOT NULL,
  "SourceKey" text NOT NULL,
  "OriginalFileName" text NULL,
  "MimeType" text NULL,
  "FileSize" bigint NULL,
  "SortOrder" integer NOT NULL,
  "CreatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_WorkVideos_WorkId_SortOrder" ON "WorkVideos" ("WorkId", "SortOrder");

CREATE TABLE IF NOT EXISTS "WorkVideoUploadSessions" (
  "Id" uuid PRIMARY KEY,
  "WorkId" uuid NOT NULL REFERENCES "Works" ("Id") ON DELETE CASCADE,
  "StorageType" text NOT NULL,
  "StorageKey" text NOT NULL,
  "OriginalFileName" text NOT NULL DEFAULT '',
  "ExpectedMimeType" text NOT NULL,
  "ExpectedSize" bigint NOT NULL,
  "Status" text NOT NULL,
  "ExpiresAt" timestamptz NOT NULL,
  "CreatedAt" timestamptz NOT NULL
);
ALTER TABLE "WorkVideoUploadSessions" ADD COLUMN IF NOT EXISTS "OriginalFileName" text NOT NULL DEFAULT '';
CREATE INDEX IF NOT EXISTS "IX_WorkVideoUploadSessions_WorkId" ON "WorkVideoUploadSessions" ("WorkId");
CREATE INDEX IF NOT EXISTS "IX_WorkVideoUploadSessions_ExpiresAt" ON "WorkVideoUploadSessions" ("ExpiresAt");

CREATE TABLE IF NOT EXISTS "VideoStorageCleanupJobs" (
  "Id" uuid PRIMARY KEY,
  "WorkId" uuid NULL,
  "WorkVideoId" uuid NULL,
  "StorageType" text NOT NULL,
  "StorageKey" text NOT NULL,
  "AttemptCount" integer NOT NULL,
  "Status" text NOT NULL,
  "LastError" text NULL,
  "CreatedAt" timestamptz NOT NULL,
  "UpdatedAt" timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_VideoStorageCleanupJobs_Status" ON "VideoStorageCleanupJobs" ("Status");
CREATE INDEX IF NOT EXISTS "IX_VideoStorageCleanupJobs_CreatedAt" ON "VideoStorageCleanupJobs" ("CreatedAt");
""";

    private const string WorkVideoMetadataSchemaPatchSql = """
ALTER TABLE "WorkVideos" ADD COLUMN IF NOT EXISTS "Width" integer;
ALTER TABLE "WorkVideos" ADD COLUMN IF NOT EXISTS "Height" integer;
ALTER TABLE "WorkVideos" ADD COLUMN IF NOT EXISTS "DurationSeconds" double precision;
ALTER TABLE "WorkVideos" ADD COLUMN IF NOT EXISTS "TimelinePreviewVttStorageKey" text;
ALTER TABLE "WorkVideos" ADD COLUMN IF NOT EXISTS "TimelinePreviewSpriteStorageKey" text;
""";

    private const string ContentSearchSchemaPatchSql = """
CREATE EXTENSION IF NOT EXISTS pg_trgm;

ALTER TABLE "Blogs" ADD COLUMN IF NOT EXISTS "SearchTitle" text NOT NULL DEFAULT '';
ALTER TABLE "Blogs" ADD COLUMN IF NOT EXISTS "SearchText" text NOT NULL DEFAULT '';
ALTER TABLE "Blogs" ALTER COLUMN "SearchTitle" SET DEFAULT '';
ALTER TABLE "Blogs" ALTER COLUMN "SearchText" SET DEFAULT '';

ALTER TABLE "Works" ADD COLUMN IF NOT EXISTS "SearchTitle" text NOT NULL DEFAULT '';
ALTER TABLE "Works" ADD COLUMN IF NOT EXISTS "SearchText" text NOT NULL DEFAULT '';
ALTER TABLE "Works" ALTER COLUMN "SearchTitle" SET DEFAULT '';
ALTER TABLE "Works" ALTER COLUMN "SearchText" SET DEFAULT '';

CREATE OR REPLACE FUNCTION public.__woongblog_content_search_normalize(value text)
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
  SELECT regexp_replace(lower(coalesce(value, '')), '[[:space:][:punct:][:cntrl:]]+', '', 'g')
$$;

UPDATE "Blogs"
SET
  "SearchTitle" = CASE
    WHEN "SearchTitle" IS NULL OR "SearchTitle" = '' THEN public.__woongblog_content_search_normalize("Title")
    ELSE "SearchTitle"
  END,
  "SearchText" = CASE
    WHEN "SearchText" IS NULL OR "SearchText" = '' THEN concat_ws(
      ' ',
      nullif(public.__woongblog_content_search_normalize("Excerpt"), ''),
      nullif(public.__woongblog_content_search_normalize("ContentJson"::text), '')
    )
    ELSE "SearchText"
  END
WHERE "SearchTitle" IS NULL
   OR "SearchTitle" = ''
   OR "SearchText" IS NULL
   OR "SearchText" = '';

UPDATE "Works"
SET
  "SearchTitle" = CASE
    WHEN "SearchTitle" IS NULL OR "SearchTitle" = '' THEN public.__woongblog_content_search_normalize("Title")
    ELSE "SearchTitle"
  END,
  "SearchText" = CASE
    WHEN "SearchText" IS NULL OR "SearchText" = '' THEN concat_ws(
      ' ',
      nullif(public.__woongblog_content_search_normalize("Excerpt"), ''),
      nullif(public.__woongblog_content_search_normalize("ContentJson"::text), '')
    )
    ELSE "SearchText"
  END
WHERE "SearchTitle" IS NULL
   OR "SearchTitle" = ''
   OR "SearchText" IS NULL
   OR "SearchText" = '';

ALTER TABLE "Blogs" ALTER COLUMN "SearchTitle" SET NOT NULL;
ALTER TABLE "Blogs" ALTER COLUMN "SearchText" SET NOT NULL;
ALTER TABLE "Works" ALTER COLUMN "SearchTitle" SET NOT NULL;
ALTER TABLE "Works" ALTER COLUMN "SearchText" SET NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_Blogs_Published_PublishedAt" ON "Blogs" ("Published", "PublishedAt");
CREATE INDEX IF NOT EXISTS "IX_Works_Published_PublishedAt" ON "Works" ("Published", "PublishedAt");
CREATE INDEX IF NOT EXISTS "IX_Blogs_SearchTitle_Trgm" ON "Blogs" USING gin ("SearchTitle" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "IX_Blogs_SearchText_Trgm" ON "Blogs" USING gin ("SearchText" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "IX_Works_SearchTitle_Trgm" ON "Works" USING gin ("SearchTitle" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "IX_Works_SearchText_Trgm" ON "Works" USING gin ("SearchText" gin_trgm_ops);

DROP FUNCTION IF EXISTS public.__woongblog_content_search_normalize(text);
""";
}
