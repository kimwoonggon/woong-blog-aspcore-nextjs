# Backend Performance Impact Map - 2026-05-06

## Goal
Identify where backend performance can still be improved and map each improvement to the Real Backend Test/backend-load metrics it should change.

## Scope
- Audit-only analysis. No production or test source code was changed.
- Baseline branch: `dev` at `c7c38d0 Optimize public hot path queries (#88)`.
- Workload semantics kept realistic: public list targets remain `pageSize=12`, read targets use current public content order, no seeded-target priority, and no cache-first recommendation.
- Runtime assumption from the user: backend, nginx, frontend, and DB share 2 CPU and 8 GiB in production-like Docker deployment.

## Current Backend Baseline
The current `dev` code has already removed the most obvious DB/query-shape waste from the four Real Backend public targets.

- Work list `/api/public/works?page=1&pageSize=12`: one Postgres window query for first page, card-field projection only, covering index added.
- Blog list `/api/public/blogs?page=1&pageSize=12`: one Postgres window query for first page, card-field projection only, covering index added.
- Blog detail: one compiled EF projection by slug, reads stored public body fields and stored cover URL.
- Work detail without videos: one compiled EF projection by slug, uses stored thumbnail/icon URLs and `VideosVersion` to avoid probing `WorkVideos`.
- Work detail with videos: one compiled Work detail query plus one compiled video-row query when `VideosVersion > 0`.
- Public Home: improved to three commands: home shell/settings/resume, featured works, recent blogs.
- Public JSON responses: source-generated JSON metadata is registered for public hot-path DTOs with reflection fallback for long-tail payloads.

## Recent Real Backend Evidence
Latest realistic local four-target runs under current target semantics:

| Run | Rate | Requests | Failed | Avg RPS | Overall P95 | P99 | DB Command P95 | DB Open P95 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `20260506-114209-public-api-rps-e40172bb` | 100rps / 30s | 3,001 | 0 | 100.0 | 5.4ms | 6.5ms | 3.9ms | 0ms |
| `20260506-114309-public-api-rps-70eb5ce2` | 300rps / 30s | 9,000 | 0 | 300.0 | 5.2ms | 10.5ms | 3.5ms | 0ms |

Target P95 at 300rps:
- Work list: 4.3ms
- Work read: 2.7ms
- Study list: 6.3ms
- Study read: 2.5ms

Interpretation:
- For constant 100-300rps, the current direct public API hot path is already structurally healthy.
- DB pool wait is not visible in these runs: DB connection-open P95 is 0ms, slow queries are 0, DB errors are 0.
- The earlier 1000rps spike problem should now be treated as saturation/capacity attribution work, not as proof that every endpoint query shape is still broken.

## Improvement Impact Map
| Priority | Improvement area | What to change | Which backend-test metric should improve | Expected impact on Real Backend Test | Risk / caveat |
|---|---|---|---|---|---|
| P0 | Heavy detail payload and serialization attribution | Measure per-target response bytes and app serialization/write time for Work/Blog detail; then trim only non-visible payload or keep stored HTML-only fast path where API compatibility allows. | `ASP.NET app elapsed p95`, client p95, GC heap delta, Gen2 GC, memory peak, target-specific Work/Study read P95, response bytes. | Best candidate if large real articles/videos make detail reads slow while DB p95 stays low. | Do not remove public-visible fields without contract tests. Markdown/HTML compatibility must be preserved. |
| P1 | Completed-run diagnostics presentation gap | Run diagnostics persistence and async DB command timing already exist. Close only remaining presentation/aggregation gaps, such as response-size, per-target diagnostics, or summary panel ambiguity. | `DB command P95/P99`, `DB connection open P95/P99`, runtime red/yellow reason, samples retained after completion, per-target attribution when available. | Does not make requests faster directly, but prevents wrong conclusions during 1000rps spike diagnosis. | Must avoid reporting only volatile latest process values as if they were run-scoped. |
| P1 | Work detail with videos | If selected Work target has videos and remains slower, reduce video detail from two DB commands to one via public video read model or Postgres JSON aggregation. | Work read P95, DB command count for video Work detail, active/open connections under spike, DB command sample count. | Helps only Work detail targets with videos. No effect on no-video Work reads. | Denormalized read model increases write-side mutation complexity; JSON aggregation can increase SQL/payload complexity. |
| P2 | Published list index verification | Run `EXPLAIN (ANALYZE, BUFFERS)` on production-like cardinality for Work/Blog list queries; adjust to partial published-only descending indexes only if planner/IO says needed. | Work list P95, Study list P95, DB command P95, buffer reads, DB CPU/IO. | Low effect on small/dev tables; relevant as content count grows. | Extra indexes cost write time and storage. Covering indexes are already present. |
| P2 | Public Home collapse/read model | If `/api/public/home` becomes a load target, collapse remaining three commands to one query/read model. | `/api/public/home` P95, DB command count 3 -> 1, DB connection-open P95. | No effect on the current four-target Real Backend Test unless Home is added. | Heterogeneous home payload makes one-query SQL or denormalized read model more complex. |
| P3 | Search path tuning | Load-test search scenarios and inspect trigram/normalized search query plans. | Search endpoint P95 and DB command P95. | No effect on current no-search list/read mix. | Do not optimize until search is in the target mix. |
| P3 | Admin/Page projections | Split or project Page/admin content where list endpoints still materialize/parses full content. | Admin/page endpoint elapsed, allocations. | No direct effect on public Real Backend Test. | Useful but lower priority for public-load objective. |

## What Should Improve When Each Slice Works
- DB/query-shape fixes should reduce DB command count, DB command P95/P99, DB open wait, and target-specific list/detail P95.
- Payload/serialization fixes should reduce `ASP.NET app elapsed p95`, GC heap/memory peaks, and detail target P95 before DB metrics change.
- Runtime/pool/nginx fixes should reduce 5xx/connection failures, dropped iterations, DB open wait, ThreadPool queue, and runtime red status under spike.
- Index fixes should mainly improve list P95 and DB buffer/IO metrics on production-sized data, not tiny local seed data.
- Diagnostics fixes should improve the reliability of the result panel, not raw throughput.

## Recommended Next Work
1. Add targeted response-size and serialization/app-write timing for public detail endpoints.
2. Run detail-heavy tests with a Work that has videos and a large Study body; only then choose between payload trimming, video read model, or runtime capacity work.
3. Close any remaining completed-run diagnostics presentation gaps only if the UI still shows ambiguous `unavailable` values despite persisted diagnostics.
4. Use `EXPLAIN (ANALYZE, BUFFERS)` for list indexes on production-like row counts before adding more indexes.

## Explicit Non-Goals
- No cache layer as the primary fix.
- No seeded target priority.
- No `pageSize=1` shortcut.
- No public DTO field removal without contract/equivalence tests.
- No claim that 1000rps spike is guaranteed on 2 CPU / shared 8 GiB after query-shape improvements alone.

## Validation Performed
- Used `analyzing-dotnet-performance` and `ef-core` guidance.
- Reviewed `WorkQueryStore`, `BlogQueryStore`, `HomeQueryStore`, Real Backend contracts/control plane, diagnostics sampler, and DB diagnostics collector.
- Reviewed current audit and load-test artifacts for public hot-path indexes, JSON source generation, and Home shell query reduction.
- Re-read current load artifacts for 100rps and 300rps realistic four-target runs.
- Checked current `dev` CI run `25433589378`; all jobs passed after report creation.
- Cross-checked the existing `real-load-run-diagnostics-2026-05-06` and `db-command-diagnostics-async-2026-05-06` reports; run diagnostics persistence and async DB command timing are already implemented, so the next measurement gap is response size/serialization/app-write attribution.
- Verified the latest performance slice promotion chain after the audit:
  - `Publish GHCR Dev` run `25433858614`: success.
  - `Promote Main Runtime` run `25433858575`: success.
  - PR #89 `Promote runtime-only tree: dev -> main`: CI success and auto-merged at `2026-05-06T12:05:15Z`.
  - main push CI run `25434162908`: success.
  - `Publish GHCR Main` run `25434455253`: success.
- Source code was not modified in this audit-only slice.

## Risks And Yellow Flags
- Current 100/300rps results are good but do not prove 1000rps spike capacity on a 2 CPU shared host.
- Current metrics already have DB diagnostics samples in `/metrics`, but the summary/result interpretation still needs stronger run-scoped attribution to avoid stale/latest-value confusion.
- Work detail with videos remains a conditional two-command path; acceptable now, but it should be measured with a real video-heavy Work target.
- Large public content may shift bottleneck from DB to response bytes/serialization/network, so query optimization alone may stop moving p95.
- Existing GitHub Actions Node.js 20 deprecation annotations and local AWSSDK.Core NU1901 warnings are unrelated but still present.

## Final Recommendation
Stop treating target-selection changes as performance work. The next useful backend step is heavy detail response-size and serialization/app-write attribution. The four current direct API targets are already healthy at 100-300rps; further improvement should be guided by spike attribution and heavy-content detail measurements.
