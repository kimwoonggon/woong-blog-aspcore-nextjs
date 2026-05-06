# Backend Performance Next Opportunities Scan - 2026-05-06

## Goal
Identify where backend performance improvements are still possible and map each improvement to the Real Backend Test/backend load-test metric it should affect.

## Scope
- Audit-only scan. Production code and tests were not changed.
- Realistic workload semantics remain fixed: public Work/Blog list `pageSize=12`, current public read targets, no seeded-target priority, and no cache as the primary fix.
- Focused paths: public Work list/read, public Blog list/read, public Home, load-test diagnostics, DB diagnostics, and DTO/read-model shape.

## Current Baseline From Code
1. Public Work list first page, no search: already one PostgreSQL command via window query.
   - Code: `WorkQueryStore.GetPublishedFirstPageWithWindowAsync`.
   - Contract test: `PublicWorkFirstPage_UsesSinglePostgresCommand_ForNoSearchList`.

2. Public Blog list first page, no search: already one PostgreSQL command via window query.
   - Code: `BlogQueryStore.GetPublishedFirstPageWithWindowAsync`.
   - Contract test: `PublicBlogFirstPage_UsesSinglePostgresCommand_ForNoSearchList`.

3. Public Blog detail: already one PostgreSQL command and reads stored public body fields.
   - Code: `BlogQueryStore.GetPublishedDetailBySlugAsync`.
   - Contract test: `PublicBlogDetail_UsesSinglePostgresCommand`.

4. Public Work detail without videos: already one PostgreSQL command and does not query `WorkVideos`.
   - Code: `WorkQueryStore.GetPublishedDetailBySlugAsync` plus `VideosVersion > 0` gate.
   - Contract tests: `PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand_AndResolverEquivalentStoredThumbnail`, `PublicWorkDetailWithoutVideos_DoesNotReferenceWorkVideosInDetailProjection`.

5. Public Work detail with videos: still can be two PostgreSQL commands.
   - First command loads Work detail fields.
   - Second command loads video DTO rows when `VideosVersion > 0`.
   - This is intentional today because videos are a collection and collapsing through a join can multiply rows.

6. Public Home: improved from five measured DB commands to three.
   - Current commands: shell page/settings/resume, featured Works, recent Blogs.
   - Contract test: `PublicHome_UsesThreePostgresCommands_ForShellAndSummaryProjections`.

7. Public list/card DTOs already avoid full body/global reads.
   - Work list selects `Id`, `Slug`, `Title`, `Excerpt`, `Category`, `Period`, `Tags`, stored public thumbnail/icon URLs, and `PublishedAt`.
   - Blog list selects `Id`, `Slug`, `Title`, `Excerpt`, `Tags`, stored public cover URL, and `PublishedAt`.
   - Detail reads stored public body fields, not admin `ContentJson` parsing on request.

## Scan Execution Checklist
Hot files scanned:
- `WorkQueryStore.cs`
- `BlogQueryStore.cs`
- `HomeQueryStore.cs`
- `DatabaseDiagnosticsCollector.cs`
- `AdminContentJson.cs`
- `ContentSearchText.cs`
- `WoongBlogDbContext.cs`
- `ContentSearchFieldSynchronizer.cs`

Pattern counts:
- Missing `StringComparison` on `IndexOf`: 0
- `Substring` allocations: 0
- `StartsWith`/`EndsWith`/`Contains` candidates: 11
- `.ToLower()`/`.ToUpper()`: 0
- `.Replace()` calls: 23
- `params` array candidates: 2
- LINQ `Select`/`Where`/`OrderBy`/`GroupBy`: 52
- LINQ `All`/`Any`: 5
- Per-call `new Dictionary<>`/`new List<>`: 4
- `static readonly Dictionary<>`: 0
- `RegexOptions.Compiled`: 7
- Per-call `new Regex(...)`: 0
- `[GeneratedRegex]`: 0
- Non-sealed public/internal classes in backend source: 34
- Sealed classes in backend source: 114

Interpretation:
- The public read hot path is already using EF projections and `AsNoTracking`.
- Most regex/string allocation signals are in admin/write-time content extraction or diagnostics sanitization, not in the normal public no-search list/detail read path.
- The remaining load-test bottleneck is more likely saturation under concurrency, payload/serialization, DB pool wait, or remaining Work-video detail work than obvious per-request string anti-patterns in the read query stores.

## Existing Load Evidence
Representative local artifacts:
- Corrected realistic heavy constant 100rps/30s: 3,001 requests, 100.0rps, failure rate 0, p95 11.3ms, dropped iterations 0.
- Corrected realistic heavy spike 100 -> 1000 -> 100rps/60s, maxVUs 1000: 26,403 requests, 330.0rps average, failure rate 3.52%, p95 3,653.5ms, dropped iterations 12,596.
- Nginx worker/pool constrained spike after runtime hardening: 36,122 requests, 451.5rps average, failure rate 0, p95 1,502.3ms, dropped iterations 2,877. This is directional, not a strict apples-to-apples comparison because target selection differed.
- Public Work list warm 300rps/30s: 9,001 requests, 300.05rps, failure rate 0, p95 4.58ms.
- Public Blog list 300rps/30s: 9,001 requests, 300.04rps, failure rate 0, p95 2.54ms.
- Public Work detail 300rps/30s on local seed-sized target: 9,001 requests, 300.03rps, failure rate 0, p95 3.99ms.
- Public Blog detail 300rps/30s on local seed-sized target: 9,001 requests, 300.02rps, failure rate 0, p95 2.96ms.
- Public Home 100rps/15s: 1,500 requests, 99.99rps, failure rate 0, p95 25.44ms.

## Prioritized Opportunities
| Priority | Opportunity | What to change | Backend test metric expected to improve | Why this is next |
|---|---|---|---|---|
| P0 | Persist and attach run-scoped DB/runtime diagnostics to completed Real Backend Test results | Capture DB command p95/p99, DB connection-open p95/p99, slow query count, ThreadPool queue, GC/memory samples into the Real Backend Test run artifact and metrics response. | `DB command P95/P99` becomes available instead of unavailable; completed result keeps diagnostic samples; future runs can distinguish DB wait from app/serialization saturation. | Without attribution, the 1000rps spike failure cannot be safely assigned to DB, app CPU, JSON serialization, nginx, or runner pressure. |
| P1 | Heavy public detail payload/serialization profiling | Add measured response-size and app serialization timing around public Work/Blog detail; only then consider source-generated JSON metadata or DTO/payload trimming that preserves visible public fields. | `ASP.NET app elapsed p95`, GC heap delta, Gen2 collections, Time in GC, process memory, client p95 under detail-heavy load. | Large Work/Study bodies are the realistic bottleneck risk after DB roundtrip reductions. This should be measured before changing serialization infrastructure. |
| P1 | Public Work detail with videos | If realistic Work read target has videos and remains slower, reduce 2-command video detail cost through a Postgres JSON aggregation projection or a public video read model, guarded by output-equivalence tests. | Work read target p95, app elapsed p95, DB command count for video Work details, DB connection-open p95, active/open connections under spike. | No-video Work detail is already optimized. Video Work detail is the remaining structurally different read path. |
| P2 | Published-list partial descending indexes | Validate with `EXPLAIN (ANALYZE, BUFFERS)` on production-like row counts; if needed, add partial indexes such as published-only `PublishedAt DESC` for Work/Blog lists. | Work list p95, Study list p95, DB command p95, DB CPU/IO pressure, active connections under larger data. | Current list path is already one command and fast locally; index work should be evidence-driven, not speculative. |
| P2 | Public Home 3 commands -> 1 command | If Home becomes a load target or high-traffic route, collapse shell + featured Works + recent Blogs into one PostgreSQL projection or a dedicated public home read model. | `/api/public/home` p95, DB command count 3 -> 1, DB connection-open p95. | Home was improved but is not part of the current four-target Real Backend Test mix. |
| P3 | Search-path tuning | Run search-specific load tests and `EXPLAIN`; tune trigram/normalized search query shape only if search is part of the performance target. | Search request p95 and DB command p95; no expected change for current no-search four-target load mix. | Current load target is no-search list/read. Search optimization will not move current results. |
| P3 | Diagnostics overhead cleanup | Replace regex constructor patterns with source-generated regex only if diagnostics sanitization shows CPU cost during high-volume slow-query recording. | Diagnostics endpoint CPU/allocations; likely no visible effect on normal public request p95. | Current regex use is static compiled and mostly not on the normal hot path. Low priority. |

## What Should Improve In Real Backend Test
If P0 is implemented:
- `DB command P95`, `DB command P99`, and DB command sample count should stop showing `unavailable` for completed runs.
- `DB connection open P95/P99` should be tied to the run instead of only being a volatile latest diagnostic.
- The result panel should make it clear whether spike latency is caused by DB pool wait, command latency, or app/runtime pressure.

If P1 payload/serialization profiling then optimization is implemented:
- `ASP.NET app elapsed p95` should drop before or along with `client p95`.
- `GC heap`, Gen2 collections, and process memory peaks should flatten under heavy detail reads.
- If response size is the dominant cost, client p95 may remain bounded by transfer size even when app elapsed improves.

If P1 Work-video detail optimization is implemented:
- Work read p95 should improve only for Work targets with videos or stale positive `VideosVersion`.
- DB command count for those Work detail requests should drop from 2 to 1 if JSON aggregation/read-model projection is chosen.
- Open/active DB connections under spike should fall if the second command was causing pool pressure.

If P2 list index tuning is implemented:
- Work list and Study list p95 should improve mainly on production-sized tables.
- On small seed/dev tables, the expected improvement is near zero and should not be used as the decision signal.

If P2 Home one-query read model is implemented:
- `/api/public/home` command count should drop from 3 to 1.
- It will not improve the current four-target Real Backend Test unless Home is added to the target mix.

## Recommended Next Implementation Order
1. P0: Real Backend Test run-scoped diagnostics persistence and DB p95/p99 in metrics/summary.
2. P1: Heavy detail response-size and serialization timing instrumentation, then optimize only the measured dominant cost.
3. P1: Work detail with-video projection/read-model reduction if a realistic video Work target remains slow.
4. P2: Production-like `EXPLAIN` for list queries before adding partial descending indexes.
5. P2: Public Home one-query read model only if Home traffic or target coverage justifies it.

## Non-Goals
- No cache layer as a primary fix.
- No seeded target priority.
- No `pageSize=1` shortcut.
- No production/test code change in this audit-only slice.
- No claim that 1000rps spike is achievable on 2 CPU / total shared 8GiB without further measurement and targeted changes.

## Validation Performed
- Read current public Work, Blog, Home query stores.
- Read current public DTO/body/read-model shape.
- Read DB diagnostics collector/interceptors and Real Backend Test result contracts.
- Scanned hot files with .NET performance anti-pattern recipes and recorded exact hit counts.
- Reviewed existing audit/load-test artifacts from 2026-05-06.
- Verified PR #83 promotion to `main` was merged; main push CI was still in progress during this scan.

## Risks And Yellow Flags
- DB command-count tests prove roundtrip reduction, not necessarily end-to-end throughput improvement under a saturated 2 CPU host.
- Local seed-sized 300rps results are useful for query-shape sanity, not final production capacity.
- Source-generated JSON or DTO reshaping should not be done blindly; correctness of public visible fields must be preserved.
- Work-video detail one-query aggregation can reduce roundtrips but may increase row/payload complexity. It needs equivalence tests.
- Index changes can hurt writes and storage if oversized; require `EXPLAIN` and production-like cardinality.

## Final Recommendation
Implement run-scoped Real Backend Test diagnostics next. Then use those metrics to decide whether the next structural optimization is heavy detail serialization/payload, Work-video detail roundtrip reduction, or published-list index tuning. Do not change test targets or add cache to hide the workload.
