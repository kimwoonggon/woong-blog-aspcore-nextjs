using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WoongBlog.Api.Infrastructure.Persistence.Seeding;

namespace WoongBlog.Api.Infrastructure.Persistence;

public static class DatabaseBootstrapper
{
    public static Task InitializeAsync(WoongBlogDbContext dbContext, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(
            async ct =>
            {
                await dbContext.Database.EnsureCreatedAsync(ct);
                await EnsureReadModelIndexesAsync(dbContext, ct);
                await EnsureAiBatchTablesAsync(dbContext, ct);
                await SeedData.InitializeAsync(dbContext);
            },
            logger,
            cancellationToken: cancellationToken);
    }

    internal static async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        ILogger? logger = null,
        int maxAttempts = 10,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        // Keep startup behavior centralized so the eventual migration-based path
        // can replace this in one place without changing the application entrypoint.
        var resolvedRetryDelay = retryDelay ?? TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await operation(cancellationToken);

                if (attempt > 1)
                {
                    logger?.LogInformation(
                        "Database bootstrap succeeded on attempt {Attempt} of {MaxAttempts}.",
                        attempt,
                        maxAttempts);
                }

                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger?.LogWarning(
                    exception,
                    "Database bootstrap attempt {Attempt} of {MaxAttempts} failed. Retrying in {RetryDelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    resolvedRetryDelay.TotalSeconds);
                await Task.Delay(resolvedRetryDelay, cancellationToken);
            }
            catch (Exception exception)
            {
                logger?.LogError(
                    exception,
                    "Database bootstrap failed on attempt {Attempt} of {MaxAttempts}.",
                    attempt,
                    maxAttempts);
                throw;
            }
        }
    }

    private static async Task EnsureReadModelIndexesAsync(WoongBlogDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        const string createIndexesSql = """
CREATE INDEX IF NOT EXISTS "IX_Works_Published_PublishedAt" ON "Works" ("Published", "PublishedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_Blogs_Published_PublishedAt" ON "Blogs" ("Published", "PublishedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_AuthSessions_RevokedAt_ExpiresAt_ProfileId" ON "AuthSessions" ("RevokedAt", "ExpiresAt", "ProfileId");
""";

        await dbContext.Database.ExecuteSqlRawAsync(createIndexesSql, cancellationToken);
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
}
