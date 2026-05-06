# Backend Performance Opportunity Scan - 2026-05-06

## Scope

User request: before making more changes, identify where backend performance can still be improved and what backend tests should prove after each improvement.

This is audit-only. No production code or test code was changed.

Non-goals:
- No cache-first solution.
- No load-test target shortcut such as seed priority or `pageSize=1`.
- No public API shape change without an explicit follow-up decision.

## Current Baseline From Code Scan

### Already improved on current `origin/dev`

1. Public Work first-page list is already a one-command Postgres window query for realistic `pageSize=12` no-search requests.
   - Code: `WorkQueryStore.GetPublishedFirstPageWithWindowAsync` uses `COUNT(*) OVER()` and projects only card fields.
   - Test: `PostgresPersistenceContractTests.PublicWorkFirstPage_UsesSinglePostgresCommand_ForNoSearchList` asserts one DB command.

2. Public Blog first-page list is already a one-command Postgres window query for realistic `pageSize=12` no-search requests.
   - Code: `BlogQueryStore.GetPublishedFirstPageWithWindowAsync`.
   - Test: `PostgresPersistenceContractTests.PublicBlogFirstPage_UsesSinglePostgresCommand_ForNoSearchList` asserts one DB command.

3. Public Blog detail is already one DB command and reads stored public body fields instead of parsing `ContentJson` on request.
   - Code: `BlogQueryStore.GetPublishedDetailBySlugAsync` projects `PublicContentHtml`, `PublicContentMarkdown`, `PublicCoverUrl`.
   - Test: `PostgresPersistenceContractTests.PublicBlogDetail_UsesSinglePostgresCommand`.

4. Public Work detail without videos is already one DB command and no longer probes `WorkVideos` just to detect existence.
   - Code: `WorkQueryStore.GetPublishedDetailBySlugAsync` uses `VideosVersion > 0`.
   - Tests: `PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand_AndResolverEquivalentStoredThumbnail`, `PublicWorkDetailWithoutVideos_DoesNotReferenceWorkVideosInDetailProjection`.

5. Admin Work list has already moved to a DTO projection using `PublicThumbnailUrl`.
   - Code: `WorkQueryStore.GetAdminListAsync` projects list fields only.
   - Test: `AdminWorkList_UsesSinglePostgresCommand_AndStoredThumbnail`.

## Prioritized Opportunities

| Priority | Area | Performance opportunity | Backend test should prove | Expected impact |
|---|---|---|---|---|
| P0 | Work thumbnail read model correctness | Keep public read path fast by computing resolver-equivalent `PublicThumbnailUrl` at write/mutation/bootstrap time, including body-image and video fallback cases where supported. | Create/update/video mutation tests prove stored `PublicThumbnailUrl == WorkThumbnailUrlResolver.ResolveThumbnailUrl(...)`; public list/detail still use one DB command and do not read `ContentJson`/`WorkVideos` for thumbnails. | Correctness guard that lets the optimized read path stay fast without response drift. Throughput gain is indirect: prevents reverting to request-time resolver work. |
| P1 | Real load diagnostics | Real Backend Test currently cannot expose DB command p95 in `RealLoadTestLatencyBreakdown`; UI can show unavailable even though the backend collector has samples. | Integration/component test proves metrics response contains DB command p95/sample count when diagnostics collector has samples; existing diagnostics endpoint remains green. | Not endpoint throughput, but crucial to know whether future changes improved DB pressure. |
| P1 | Public home endpoint | `GetHomeQueryHandler` executes home page, site settings, resume asset, featured works, and recent posts sequentially; site settings also does a separate asset lookup for resume. | New Postgres contract test records command count for public home; after improvement it should drop by at least one command, e.g. site settings + resume URL in one projection. | Reduces DB roundtrips on `/api/public/home`; not part of current 4-target load test, but important public traffic. |
| P2 | Public Work detail with videos | Detail with videos is intentionally 2 commands now: Work row + video rows. If video-heavy works dominate, denormalizing public video DTO metadata could reduce it to 1 command. | Test with a published Work containing videos asserts command count target: current 2 commands accepted; if denormalized, 1 command and no `WorkVideos` read. | Potentially meaningful only for video-heavy Work detail targets. Riskier because video mutations must maintain another read model. |
| P2 | Page/admin content projections | `PageQueryStore.GetAdminPagesAsync` materializes full `PageEntity` rows and parses `ContentJson` after materialization. | Contract test should prove list/admin page summary queries select only DTO-visible columns or stored public fields once split exists. | Allocation/CPU reduction for admin/page reads; lower priority for public load test. |
| P3 | JSON serialization allocation | Load-test reports and large public detail DTOs use normal reflection-based JSON serialization. | Add benchmark or allocation-focused component guard only if profiling shows serialization dominates; ordinary xUnit will not reliably prove this. | Possible CPU/allocation reduction, but should follow profiling because API payload size may dominate. |
| P3 | Search query tuning | Search path uses `Contains` over normalized columns with trigram indexes present. No-search path is already optimized. | Query-shape test for search only if search load is a target. | Low for current no-search load targets. |

## Key Findings

### 1. The user's thumbnail risk is real, but it is a correctness/safety gate rather than a direct throughput win

Current create/update handlers set `PublicThumbnailUrl` only from an explicit thumbnail asset:

- `CreateWorkCommandHandler`: `PublicThumbnailUrl = ResolvePublicMediaUrl(request.ThumbnailAssetId, assetPublicUrls)`.
- `UpdateWorkCommandHandler`: same pattern.

Old resolver behavior can also derive a thumbnail from:
- preferred Work video, especially YouTube `https://img.youtube.com/vi/{id}/hqdefault.jpg`
- first image in body HTML when there is no explicit asset and no video

Current public read path uses stored `PublicThumbnailUrl` directly. That is good for performance, but fallback equivalence must be maintained at write time if those fallback semantics are still required.

Recommended test-first slice:
- RED: create a Work with no thumbnail asset but body image, then assert stored/public thumbnail equals resolver result.
- RED: add YouTube video to a Work without explicit thumbnail, then assert stored/public thumbnail equals resolver result.
- GREEN: centralize write-model refresh of public Work thumbnail; call it from create/update/video add/delete/reorder/bootstrap where needed.
- Guard: public list/detail command count must stay 1 for no-video detail and first-page list.

### 2. Public list/detail DB roundtrip tests are already the strongest backend performance signal

The existing Postgres tests are valuable because they prove observable backend performance properties:
- one command for public Work detail without videos
- no `WorkVideos` reference for no-video detail projection
- one command for public Work first page at `pageSize=12`
- one command for public Blog detail/list
- one command for admin Work list projection

Those tests should be preserved and extended, not replaced by lower-level mocks.

### 3. Home endpoint still has measurable roundtrip reduction potential

`GetHomeQueryHandler` calls the store sequentially:
- home page
- site settings
- featured works
- recent posts

`HomeQueryStore.GetSiteSettingsSummaryAsync` can additionally query `Assets` for the resume URL. That makes public home a 4-5 command path depending on resume asset.

A safe first improvement is to collapse site settings + resume asset into one projection/left join or stored resume public URL. Backend test should record command count before and after using the same `LoadTestDbCommandDiagnosticsInterceptor` pattern as public list/detail tests.

### 4. Work detail with videos is the next real public detail hotspot only if selected targets contain videos

Current Work detail:
- no videos: 1 DB command
- with videos: 2 DB commands

This matches the current 1-2 roundtrip target. Reducing video detail to 1 command requires a denormalized public video read model. That can help if the real load target is a video-heavy Work, but it increases write-side complexity.

Recommendation: do not implement this until a test target with videos shows materially worse app elapsed p95 than no-video detail.

### 5. Real Backend Test needs DB timing connected to the result model

The backend has command latency collection via `LoadTestDbCommandDiagnosticsInterceptor` and `DatabaseDiagnosticsCollector`, but `RealLoadTestLatencyBreakdown` does not include DB command p95/sample count. `K6RealLoadTestRunner.ApplyK6SummaryAsync` only writes client, app, and nginx-derived timing.

This should be treated as an observability improvement. Without it, future load-test regressions cannot be attributed cleanly to DB pressure from the real backend result panel.

## Recommended Next Implementation Order

1. P0: Work thumbnail read-model equivalence for fallback cases.
   - Reason: protects response correctness while keeping the optimized public read path.
   - Expected backend tests: resolver-equivalent stored URL on create/update/video mutation; public list/detail command count remains optimized.

2. P1: Real load DB command p95 in metrics response.
   - Reason: makes future backend performance changes measurable in the UI/test artifacts.
   - Expected backend tests: metrics response includes DB p95/sample count when collector has samples.

3. P1: Public home DB roundtrip reduction.
   - Reason: concrete public endpoint with avoidable sequential DB commands.
   - Expected backend tests: command count reduction for home path and no asset-row dependency for featured/recent media.

4. P2: Work detail with videos read-model denormalization only after measurement shows it is the bottleneck.

## Validation Performed

- Code scan of public Work, Blog, Home, Page, SiteSettings query stores.
- Code scan of Work/Blog create/update handlers and Work video mutation handlers.
- Code scan of DB diagnostics collector/interceptor and real load runner contracts.
- Existing Postgres performance tests mapped to current optimized paths.
- No production/test source files modified.

## Risks / Yellow Flags

- Fallback thumbnail equivalence can conflict with the earlier decision to exclude body/video fallback from public hot paths. The right implementation is write-time denormalization, not read-time resolver restoration.
- DB command count is an excellent test signal for roundtrips, but it does not prove serialization allocation or payload size improvements.
- Source-generated JSON or DTO shape reduction should not be prioritized until app elapsed p95 or profiling shows serialization dominates.
- Home endpoint optimization may require interface changes in `IHomeQueryStore`; keep it as a separate slice.

## Final Recommendation

Proceed with P0 first: add resolver-equivalence tests for thumbnail fallback paths and make the stored public thumbnail read model correct at mutation/bootstrap time while preserving one-command public list/detail reads. Then add DB p95 to real load metrics so subsequent performance work is attributable.
