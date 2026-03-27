using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Infrastructure.Persistence.Seeding;

namespace Portfolio.Api.Infrastructure.Persistence;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(PortfolioDbContext dbContext, CancellationToken cancellationToken = default)
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
                await SeedData.InitializeAsync(dbContext);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private static async Task EnsureAiBatchTablesAsync(PortfolioDbContext dbContext, CancellationToken cancellationToken)
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
