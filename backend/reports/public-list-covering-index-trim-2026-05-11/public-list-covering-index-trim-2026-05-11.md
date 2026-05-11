# Public List Covering Index Trim Audit - 2026-05-11

## Summary
This slice makes the public Work list PostgreSQL covering index match the current public DTO/read shape. The public Work list no longer exposes `period` or `iconUrl`, so the backend bootstrap now rebuilds `IX_Works_PublicList_Covering` without `Period` and `PublicIconUrl`, while preserving `PublicThumbnailUrl` for the existing thumbnail contract.

## Changed
- Added schema patch `20260511_public_work_list_covering_index_visible_fields` in `DatabaseBootstrapper`.
- Rebuilt `IX_Works_PublicList_Covering` as `("Published", "PublishedAt" DESC) INCLUDE ("Id", "Slug", "Title", "Excerpt", "Category", "Tags", "PublicThumbnailUrl")`.
- Updated PostgreSQL persistence contract tests to assert the Work public list index includes `PublicThumbnailUrl` and excludes `Period` and `PublicIconUrl`.
- Added a regression test proving an existing legacy wide index is dropped and recreated by bootstrap.
- Fixed `admin-home-image-upload.spec.ts` to wait for `/revalidate-public` after saving, removing a real full-suite race.
- Recorded dev Real Backend Test artifacts under `backend/reports/public-list-covering-index-trim-2026-05-11/loadtest/`.

## Intentionally Not Changed
- No cache layer was added.
- `pageSize=12` was preserved for public Work/Study list load targets.
- No seed or fixture target was forced for Real Backend Test.
- No production SSH, production deploy, or remote server operation was performed.
- No HLS upload/transcoding code was changed in this slice.
- Public DTO contracts were not loosened; public Work list still only returns visible list fields.

## Goal Verification
- Backend structure improvement: satisfied. The database index now matches the trimmed public list projection and avoids carrying removed public fields in the hot list index.
- Real Backend Test conditions: satisfied for dev. The run used `pageSize=12`, no seed override, no cache setting, and real current public Work/Study URLs selected from public API responses.
- E2E functionality: satisfied for dev. Full Playwright E2E rerun passed after stabilizing the home image revalidation race.
- Production/server scope: intentionally not performed, per user instruction to ignore production SSH/server work.

## Validation Performed
- RED focused integration test: `PostgresPersistenceContractTests.Bootstrapper_AppliesPostgresSpecificSearchSchema` failed before implementation because `Period` was present in the index definition.
- Focused GREEN integration tests: 2/2 passed for fresh bootstrap and legacy wide-index rebuild.
- Backend solution: `dotnet test backend/WoongBlog.sln --verbosity minimal` passed. Contract 1 skipped, Component 130 passed, Unit 61 passed, Architecture 40 passed, Integration 232 passed.
- Frontend typecheck: `npm run typecheck` passed.
- Lint: `npm run lint` passed with 0 errors and 5 pre-existing warnings.
- Dev compose: stack built and ran on `http://127.0.0.1:3006`; `/api/health` returned 200.
- Container DB inspection: `IX_Works_PublicList_Covering` excludes `Period` and `PublicIconUrl`, includes `PublicThumbnailUrl`.
- Full E2E: rerun passed with 430 passed, 4 skipped, 0 budget failures.
- Real Backend Test dev run: `20260511-042239-public-api-rps-38326f7e`, completed, 2000 requests, 0 failed, 100 average rps, p95 2.1 ms, p99 2.6 ms.

## Real Backend Test Targets
- Work list: `/api/public/works?page=1&pageSize=12`, 500 requests, p95 2.1 ms, db command p95 0.8 ms, 2xx 500.
- Work read: `/api/public/works/video-only-refresh-1778472933526`, 500 requests, p95 1.9 ms, db command p95 0.7 ms, 2xx 500.
- Study list: `/api/public/blogs?page=1&pageSize=12`, 500 requests, p95 2.1 ms, db command p95 0.8 ms, 2xx 500.
- Study read: `/api/public/blogs/saved-blog-1778472860213`, 500 requests, p95 1.9 ms, db command p95 0.6 ms, 2xx 500.

## Risks And Yellow Flags
- The schema patch uses `DROP INDEX` plus `CREATE INDEX`, not `CONCURRENTLY`. On a very large `Works` table this can add startup/index rebuild time. Current expected dataset size makes this acceptable for bootstrap, but it should be considered if the table grows significantly.
- The Real Backend Test is a dev/local validation, not a 2 CPU / 8 GiB production-capacity proof.
- `npm ci` reported existing audit vulnerabilities; this slice did not address dependency security drift.
- Lint still has pre-existing warnings unrelated to this slice.

## Final Recommendation
Open a dev PR for this slice and let CI verify the same contracts. If CI passes, merge to `dev`. Do not promote to production or touch remote server state as part of this slice.
