# Public Work Detail Video Existence Query Removal - 2026-05-06

## Summary
Removed the request-time `WorkVideos` existence subquery from the public Work detail no-video hot path. Public detail now uses the stored `Works.VideosVersion` value to decide whether a second video DTO query is necessary.

## Changed
- Added a Postgres contract test proving no-video public Work detail SQL does not reference `WorkVideos`.
- Changed `WorkQueryStore.GetPublishedDetailBySlugAsync` from `dbContext.WorkVideos.Any(...)` in the projection to `work.VideosVersion > 0`.
- Added one-time relational schema patch `20260506_work_video_version_backfill` to set `VideosVersion = 1` for existing Work rows that already have WorkVideos but still have version 0.
- Updated seeded Work data to carry `VideosVersion = 1` when seeded videos exist.
- Aligned one public HLS detail component fixture with the current mutation-produced state by setting `VideosVersion = 1`.

## Intentionally Not Changed
- No cache was introduced.
- No Real Backend Test target selection was changed.
- No seeded target priority was added.
- Public list targets remain `pageSize=12`.
- Public Work detail with videos can still perform a second query to load video DTOs; the target remains 1 query for no-video detail and 2 queries for video detail.
- Existing thumbnail resolver equivalence guards remain in place and were not weakened.

## Goal Verification
- Goal: improve backend structure for realistic public detail reads without faking the workload.
- Result: no-video public Work detail no longer asks Postgres to evaluate `EXISTS (SELECT 1 FROM "WorkVideos" ...)` during the detail projection.
- Safety guard: legacy/current data with existing `WorkVideos` and `VideosVersion = 0` is backfilled once at startup, so video detail rows are not silently hidden after switching to the stored flag.

## Validations
- PASS: RED no-video SQL-shape test failed before implementation because generated SQL contained `FROM "WorkVideos"`.
- PASS: RED backfill test failed before implementation because legacy Work rows with videos stayed at `VideosVersion = 0`.
- PASS: focused Postgres backfill + no-video SQL-shape tests passed 2/2.
- PASS: focused public video detail + WorkVideo component tests passed 27/27.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore` passed 12/12.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --no-restore` passed 122/122.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` completed successfully; observed Component 122/122 and Integration 216/216 output.
- PASS: explicit Unit 56/56, Architecture 35/35, Contract 1 skipped due no Pact files.
- PASS: `git diff --check`.
- NOT RUN: local load smoke; `docker compose -f docker-compose.dev.yml ps` showed no running services.

## Risks And Follow-Up
- `VideosVersion > 0` is a coarse "may have videos" flag. After all videos are deleted it can remain positive, so some historical rows may still do a second query that returns empty. That is acceptable for correctness and still within the 1-2 query target.
- Direct database writes that add WorkVideos after the backfill without incrementing `VideosVersion` can still bypass the stored flag. Application mutation handlers already increment the version; direct writes should not be used for runtime content mutation.
- Existing AWSSDK.Core NU1901 warnings remain unrelated.

## Recommendation
Merge this slice before continuing to JSON serialization allocation and remaining timing instrumentation work. It is a safe structural improvement: lower no-video detail query pressure without cache or fake test targets.
