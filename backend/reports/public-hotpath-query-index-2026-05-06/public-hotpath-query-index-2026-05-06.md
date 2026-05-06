# Public Hot Path Query Indexes And Compiled Reads Audit

Date: 2026-05-06
Branch: perf/public-hotpath-query-index-2026-05-06

## Goal
Improve structural backend performance for realistic public Work/Blog list and detail load-test targets without changing target realism.

## Changes
- Added Postgres schema patch `20260506_public_list_covering_indexes`.
- Added covering index `IX_Blogs_PublicList_Covering` on `Blogs(Published, PublishedAt DESC)` including public list projection columns.
- Added covering index `IX_Works_PublicList_Covering` on `Works(Published, PublishedAt DESC)` including public list projection columns.
- Converted public Blog detail read projection to EF compiled query, cached per EF `IModel`.
- Converted public Work detail read projection to EF compiled query, cached per EF `IModel`.
- Converted public Work video public read projection to EF compiled query, cached per EF `IModel`.
- Kept existing one-command public Blog detail and no-video Work detail contracts.

## Intentionally Not Changed
- No cache or output cache was added.
- No Real Backend target seed priority was added.
- No list `pageSize=1` shortcut was added; tested target remains `pageSize=12`.
- No public DTO fields were removed.
- No frontend behavior was changed.
- No admin query path was optimized in this slice.

## Goal Verification
- Public list endpoints retain realistic `pageSize=12` semantics.
- Public list DB can use covering indexes for card projection fields when Postgres planner selects index-only scans under larger tables.
- Public detail paths avoid repeated LINQ translation overhead after first query compilation per EF model.
- Work detail with no videos still avoids the WorkVideos query.
- Work detail with videos remains at most one detail command plus one videos command.

## Validations
- RED focused Postgres schema contract failed before implementation because covering indexes were absent.
- PASS: focused schema contract after new patch.
- PASS: focused public detail/list Postgres command-count contracts.
- PASS: `PublicQueryHandlerComponentTests` 40/40.
- PASS: `PublicEndpointsTests` 29/29.
- PASS: `PostgresPersistenceContractTests` 14/14.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"`: Contract 1 skipped, Component 122, Unit 56, Architecture 35, Integration 224.
- PASS: `git diff --check`.
- PASS: diff guard found no `pageSize=1`, seeded target priority, or cache-related change.
- PASS: dev compose rebuilt with `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh`.
- PASS: nginx and backend direct health checks returned 200.
- PASS: Real Backend 100rps/30s/maxVUs 100 with current real public target semantics: run `20260506-114209-public-api-rps-e40172bb`, 3,001 requests, 0 failed, p95 5.4ms, p99 6.5ms, DB command p95 3.9ms, DB connection open p95 0ms.
- PASS: Real Backend 300rps/30s/maxVUs 300 with current real public target semantics: run `20260506-114309-public-api-rps-70eb5ce2`, 9,000 requests, 0 failed, p95 5.2ms, p99 10.5ms, DB command p95 3.5ms, DB connection open p95 0ms.

## Runtime Evidence
- 100rps artifacts: `backend/reports/public-hotpath-query-index-2026-05-06/loadtest/runtime-summary.json` and `runtime-metrics.json`.
- 300rps artifacts: `backend/reports/public-hotpath-query-index-2026-05-06/loadtest/runtime-300rps-summary.json` and `runtime-300rps-metrics.json`.
- Target discovery artifact: `backend/reports/public-hotpath-query-index-2026-05-06/loadtest/targets.json`.

## Risks And Yellow Flags
- Covering indexes add write-time index maintenance cost and additional storage. Public content writes are admin-side and low frequency, so this is acceptable for read-heavy public load.
- Postgres index-only scans depend on planner choice and visibility map state; the index is an enabler, not a hard guarantee for every run.
- EF compiled queries are cached per `IModel` to avoid cross-model failures. This adds small static dictionaries but keeps production hot path compiled after first use.
- Existing AWSSDK.Core NU1901 low severity warning remains unrelated.

## Recommendation
Merge this slice to `dev`, verify CI, then promote to `main`. Next structural improvement should evaluate whether public detail payload can safely move to pre-rendered HTML-only for new content while retaining legacy markdown compatibility, because large detail response size remains the main unavoidable cost under real large articles.
