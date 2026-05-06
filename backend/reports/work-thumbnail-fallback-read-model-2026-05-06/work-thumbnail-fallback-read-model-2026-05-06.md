# Work Thumbnail Fallback Read Model - Audit Report

Date: 2026-05-06
Branch: `perf/work-thumbnail-fallback-equivalence-2026-05-06`

## Goal
Preserve the old `WorkThumbnailUrlResolver` fallback semantics without putting body JSON parsing or video fallback resolution back into public Work list/detail read paths. The performance direction is write-time computation: public endpoints should read the stored `Works.PublicThumbnailUrl` value directly.

## Changed
- Added `WorkPublicThumbnailReadModel` as the single write-side refresher for `Work.PublicThumbnailUrl`.
- Updated Work create/update handlers to compute `PublicThumbnailUrl` from explicit thumbnail asset, existing videos, or body image fallback at mutation time.
- Updated Work video add/confirm/HLS/delete/reorder handlers to refresh the stored thumbnail read model when video ordering or presence changes.
- Added video-command asset URL lookup so unresolved explicit thumbnail asset IDs still fall back exactly like the old resolver.
- Added Postgres bootstrap patch `20260506_public_work_thumbnail_fallback_backfill` to backfill legacy fallback rows.
- Added integration and Postgres contract coverage for body-image fallback, YouTube fallback, unresolved-asset fallback, video delete/reorder recomputation, and bootstrap backfill.

## Intentionally Not Changed
- Public Work query paths were not changed to call `WorkThumbnailUrlResolver` at request time.
- Real Backend Test targets were not changed: no seeded target priority and no `pageSize=1` shortcut.
- No cache/`IMemoryCache` workaround was introduced.
- Public DTO shape was not expanded with admin-only fields.
- Blog thumbnail/cover behavior was not changed in this slice.
- Runtime nginx/Postgres pool settings were not changed in this slice.

## Verification Against Goals
- Stored `PublicThumbnailUrl` is now refreshed on Work create/update and video mutations.
- The strict resolver-equivalence edge with unresolved explicit thumbnail asset IDs is covered and passing.
- Public query guard confirms this slice did not add seeded/pageSize/cache changes.
- `WorkQueryStore` still projects `PublicThumbnailUrl` for public list/detail; the remaining resolver call is in admin detail, not the public hot path.

## Validations Performed
- RED then GREEN: `AdminContentEndpointsTests.CreateWork_StoresBodyImageThumbnailFallback_ForPublicReadModel`.
- RED then GREEN: `AdminContentEndpointsTests.UpdateWork_RecomputesBodyImageThumbnailFallback_ForPublicReadModel`.
- RED then GREEN: `WorkVideoEndpointsTests.AddYouTubeVideo_PersistsAndProjectsToAdminAndPublic`.
- RED then GREEN: `PostgresPersistenceContractTests.Bootstrapper_BackfillsPublicThumbnailUrl_FromLegacyFallbacks`.
- RED then GREEN: unresolved explicit thumbnail asset tests for update and video mutation.
- PASS: focused unresolved-asset tests 2/2.
- PASS: focused integration filter 61/61 for `AdminContentEndpointsTests`, `WorkVideoEndpointsTests`, and `PostgresPersistenceContractTests`.
- PASS: focused component filter 65/65 for `PublicQueryHandlerComponentTests` and `WorkVideoComponentTests`.
- PASS: `dotnet test backend/WoongBlog.sln --logger "console;verbosity=minimal"`: Component 122/122, Unit 56/56, Architecture 35/35, Integration 222/222, Contract 1 skipped due missing pact files.
- PASS: `git diff --check`.
- PASS: diff guard found no new `pageSize=1`, seeded target priority, or cache workaround in this slice.
- NOT RUN: local compose load smoke, because `docker compose -f docker-compose.dev.yml ps` showed no running services.

## Expected Backend Test / Runtime Effect
- Correctness: public Work thumbnail output can match the old resolver without reintroducing request-time body JSON scans.
- Performance: public Work list/detail can continue reading the stored `PublicThumbnailUrl` column instead of parsing large Work body JSON to discover fallback images.
- Capacity expectation after deployment: lower CPU/allocation risk on heavy Work read targets that rely on representative-image fallback; no artificial improvement from smaller page size, seed slugs, or cache.

## Risks / Yellow Flags
- Video mutations now perform an admin-side asset URL lookup and may load videos when fallback is needed. This is intentional because admin mutations are not the public load-test hot path.
- Existing legacy rows with unusual HTML forms outside the tested `<img src="...">` and `<img src='...'>` patterns may not be backfilled, but future writes use the C# resolver.
- Local runtime load was not rerun for this slice; the next validation after deployment should compare real backend Work read p95, app elapsed p95, Gen2 GC, and DB active/open connection behavior.

## Final Recommendation
Proceed with PR to `dev` after local audit artifacts are committed. After CI/deployment, rerun Real Backend Test with realistic `pageSize=12` targets and current public read slugs to measure whether heavy Work read p95 and GC pressure improve without target or cache changes.
