# Public Media Read Model - 2026-05-06

## Objective
Improve public backend read performance without using cache by removing avoidable request-time `Assets` lookups from public Work, Blog, and Home read paths. Keep realistic load-test semantics: no seed-priority target selection and no `pageSize=12` reduction.

## Changed
- Added stored public media URL fields:
  - `Work.PublicThumbnailUrl`
  - `Work.PublicIconUrl`
  - `Blog.PublicCoverUrl`
- Updated EF model defaults and required-field contract tests for the stored URL fields.
- Added a relational bootstrap schema patch, `20260506_public_media_url_fields`, to add columns and backfill from existing `ThumbnailAssetId`, `IconAssetId`, and `CoverAssetId` relationships.
- Updated Work create/update command paths to resolve selected asset IDs into stored public URLs at write time.
- Updated seed data and public detail seed rehydration to populate stored public media URLs.
- Updated direct SQL Notion import to write `PublicCoverUrl` with `CoverAssetId`.
- Refactored public query stores so Work list/detail, Blog list/detail, Home featured works, and Home recent posts project stored URL columns directly instead of performing public-card/detail asset lookup roundtrips.
- Added/updated component and integration tests proving public read paths use stored URLs and do not require runtime `Assets` rows for public media URLs.
- Registered the `optimizing-ef-core-queries` skill in the repo skill lock after using it for this EF Core projection/read-path slice.

## Intentionally Not Changed
- No response/output/server-side cache was added.
- No Real Backend Test target was changed to prefer seed-only content.
- Public list `pageSize=12` semantics were not reduced or hidden behind a test shortcut.
- Blog admin create/update API still does not expose cover asset selection; blog cover URL population remains covered through seed/bootstrap/import paths and existing persisted rows.
- The resume asset lookup in Home site settings remains asset-backed because it is a single site-setting file reference and not part of repeated public content card media resolution.
- The local smoke database still uses seeded local content; production content distribution must be measured after deployment.

## Goal Verification
- Public Work list/detail/home no longer need request-time `Assets` lookup for thumbnail/icon URLs.
- Public Blog list/detail/home no longer need request-time `Assets` lookup for cover URLs.
- Stored URL fields are populated for writes/backfill/import/seed paths that can create or update representative media references.
- Existing public DTO shape is preserved; callers still receive `thumbnailUrl`, `iconUrl`, and `coverUrl`.
- The implementation is denormalized read-model storage, not cache: URL values are persisted content fields and backfilled from canonical asset references.

## Validations
- RED: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~GetHomeQueryHandler_UsesStoredPublicMediaUrlsWithoutAssetRows" --no-restore` initially failed before stored URL fields existed.
- PASS: focused component/model tests passed 44/44 for public query handlers and DbContext model contracts.
- PASS: focused integration tests passed 103/103 for public endpoints, admin content/mutation endpoints, database bootstrapper, Postgres persistence contracts, and startup composition.
- PASS: `npm run test -- --run src/test/notion-db-import-lib.test.ts` passed 1 file, 6 tests.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed Unit 56/56, Architecture 35/35, Component 122/122, Integration 205/205, Contract 1 skipped due no pact files.
- PASS: `npm run lint` completed with 0 errors and 6 pre-existing warnings.
- PASS: `git diff --check` completed with no whitespace errors.
- PASS: prod-like Docker stack rebuilt from this branch with backend/frontend/db/nginx pinned to `cpuset=0,1` and memory split backend 3GiB, DB 3GiB, frontend 1GiB, nginx 512MiB.
- PASS: k6 300rps/30s smoke completed with 0 failures on four target shapes:
  - Works list `pageSize=12`: 9,001 requests, 300.01 rps, p95 7.48 ms.
  - Blogs list `pageSize=12`: 9,000 requests, 300.03 rps, p95 4.87 ms.
  - Work detail `seeded-work`: 9,001 requests, 300.03 rps, p95 3.99 ms.
  - Blog detail `seeded-blog`: 9,001 requests, 300.02 rps, p95 2.96 ms.
- PASS: k6 summary exports saved under `backend/reports/public-media-read-model-2026-05-06/loadtest/`.

## Risks And Yellow Flags
- Stored URL fields can become stale if an asset public URL is mutated after content rows reference it. Current code treats asset public URLs as stable; if that changes, add an asset-update propagation path.
- Local load smoke uses seeded local data, not production-sized real articles/videos. It validates query shape and runtime stability under constrained Docker, not final production capacity.
- A backend runtime warning still appears in Alpine logs for missing `libgssapi_krb5.so.2`; health and load smoke passed, but this should be tracked separately if it appears in production logs.
- The installed EF Core skill adds repo metadata/files; keep it only if the team wants persistent skill registration in the repo.

## Final Recommendation
Promote this slice through `dev` CI, then `main`, then rerun the Real Backend Test against the production-like host with the same four realistic targets and `pageSize=12`. The next backend performance slice should inspect remaining high-load bottlenecks using the now-available DB command diagnostics: likely DB connection saturation under spike, detail payload/content size, and ThreadPool queue behavior under 100 -> 1000 rps.
