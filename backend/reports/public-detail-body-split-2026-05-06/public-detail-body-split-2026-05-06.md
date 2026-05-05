# Public Detail Body Split Audit - 2026-05-06

## Goal
Improve backend structure for realistic public load-test targets without cache, seed-only target selection, or `pageSize=12` reduction. The slice focuses on public Work/Blog detail reads so public requests no longer read or return admin editing JSON wholesale.

## Changed
- Added stored public body fields on `Blog` and `Work`: `PublicContentHtml` and `PublicContentMarkdown`.
- Added write-time synchronization from `ContentJson` into the stored public body fields during EF `SaveChanges`.
- Added database bootstrap schema patch and backfill for the new public body fields.
- Changed public Blog/Work detail DTOs from root `contentJson` to root `content` with renderable public body fields.
- Changed public Blog/Work detail query stores to project only stored public body fields instead of full admin `ContentJson`.
- Updated frontend public detail clients/pages/helpers to consume `content` while keeping a legacy `contentJson` fallback during rollout.
- Updated Pact and backend/frontend tests for the public detail contract change.
- Updated direct SQL Notion blog import so it writes `PublicContentHtml`/`PublicContentMarkdown` together with `ContentJson`, preventing imported posts from bypassing EF synchronization.

## Intentionally Not Changed
- No cache was added.
- Realistic public list targets were not reduced: `pageSize=12` remains in real backend load-test defaults.
- No seed-only target selection was introduced.
- Admin read/edit payloads still use admin content JSON where needed.
- Public list/card endpoints still return card DTOs only; this slice did not rewrite pagination semantics.
- Work public detail still returns videos; video DTO/query cost remains a separate follow-up.

## Verification Against Goals
- Public detail no longer exposes root `contentJson` for Work/Blog detail responses.
- Public detail DB projection no longer selects full `ContentJson` for Work/Blog detail body rendering.
- Existing public list realism is preserved.
- Direct import consistency was added so imported blog posts populate the new public body fields.
- Cache was not used as an optimization.

## Validations Performed
- RED: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~GetWorkBySlug_ReturnsPublicContentBodyWithoutAdminContentJson|FullyQualifiedName~GetBlogBySlug_ReturnsPublicContentBodyWithoutAdminContentJson" --no-restore` failed before implementation because public detail still exposed `contentJson`.
- GREEN: same focused backend integration tests passed after implementation, 2/2.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests|FullyQualifiedName~DbContextModelContractComponentTests" --no-restore`, 43/43.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests|FullyQualifiedName~AdminContentEndpointsTests|FullyQualifiedName~PersistenceContractTests" --no-restore`, 57/57.
- PASS: focused frontend/Pact/detail Vitest suite, 9 files and 51 tests.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore`, component 121/121, unit 56/56, architecture 35/35, integration 204/204, contract 1 skipped because no pact files.
- PASS: `npm run test -- --run src/test/notion-db-import-lib.test.ts`, 1 file and 6 tests.
- PASS: `npm run typecheck`, 0 TypeScript errors.
- PASS: `npm run lint`, 0 errors and 6 pre-existing warnings.
- PASS: `git diff --check`, no whitespace errors.
- SCAN: hot public query stores use `AsNoTracking` and projected row DTOs for list/detail paths.

## Risks And Follow-Ups
- Performance load test has not yet been rerun on a rebuilt current-branch compose stack with the 2 CPU / 8GB shared constraint. This is required before claiming latency/RPS improvement.
- Work public detail still queries and serializes Work videos. For Work pages with large video metadata, this remains the next likely detail-path cost after body split.
- Public list still performs `CountAsync` plus page query. At high RPS this is two DB round trips per list request; consider deferred/approximate count or cursor-style pagination only if product semantics allow it.
- Search uses `Contains` over normalized search fields. Need PostgreSQL `EXPLAIN ANALYZE` on production-like data to confirm trigram indexes are actually used under realistic query terms.
- DB command P95 is still unavailable in the load-test UI in the reported runs. Instrumentation must expose command timing before DB-vs-app attribution can be trusted.
- Admin content parsing helpers still contain regex/replace chains, but public reads no longer parse full content JSON on every detail request after this slice.

## Final Recommendation
Merge this slice only after CI is green, then rebuild the 2 CPU / 8GB compose stack and rerun the same realistic public-api-mix/spike tests with `pageSize=12` and non-seed detail targets. Next backend optimization target should be Work detail video projection/serialization and DB timing observability, not cache.
