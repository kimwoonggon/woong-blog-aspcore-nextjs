# Backend Performance Opportunity Map - 2026-05-07

## Scope
사용자가 요청한 내용은 현재 backend 구조에서 추가 성능 개선이 가능한 지점을 파악하고, 각 개선이 Real Backend Test의 어떤 수치를 움직일지 연결하는 것이다.

이번 작업은 audit-only slice이다. 생산 코드와 테스트 코드는 수정하지 않았고, 코드 스캔/기존 PR CI/targeted PostgreSQL 계약 테스트/GitHub PR 상태만 확인했다.

## Baseline
현재 기준선은 `dev` commit `6a18e20`이다.

- PR #100: `Add public work video read model` merged to `dev` as `5e3b898`.
- PR #102: `Fast path empty public work videos` merged to `dev` as `6a18e20` after all PR checks passed.
- Earlier stale 판단이던 “Work detail with videos = 2 DB commands”는 더 이상 현재 기준선이 아니다. 현재 Work detail with videos도 1 DB command가 계약이다.

## Real Backend Test Hot Paths
Real Backend Test의 기본 public API mix는 다음 4개 경로다.

- Work list: `/api/public/works?page=1&pageSize=12`
- Work read: `/api/public/works/{slug}`
- Study/Blog list: `/api/public/blogs?page=1&pageSize=12`
- Study/Blog read: `/api/public/blogs/{slug}`

## Current Hot Path State
현재 코드 기준 확인 사항:

- Work list 첫 페이지, 검색 없음: PostgreSQL raw window query 1회로 list와 total count를 같이 가져온다. `pageSize=12`는 유지된다. Evidence: `WorkQueryStore.GetPublishedFirstPageWithWindowAsync`.
- Blog list 첫 페이지, 검색 없음: PostgreSQL raw window query 1회로 list와 total count를 같이 가져온다. Evidence: `BlogQueryStore.GetPublishedFirstPageWithWindowAsync`.
- Blog detail: compiled projection query 1회로 `PublicContentHtml`, `PublicContentMarkdown`, `PublicCoverUrl`만 읽는다. Evidence: `BlogQueryStore.CreatePublishedDetailBySlugQuery`.
- Work detail without videos: compiled projection query 1회이고 `WorkVideos`를 조회하지 않는다. Evidence: `WorkQueryStore.CreatePublishedDetailBySlugQuery`, `WorkPublicVideosReadModel.Deserialize` empty JSON fast path.
- Work detail with videos: compiled Work projection query 1회로 `PublicVideosJson` snapshot까지 읽는다. Evidence: `WorkQueryStore.CreatePublishedDetailBySlugQuery`, `PublicWorkDetailWithVideos_UsesSinglePostgresCommand_AndStoredPublicColumnsOnly`.
- Public detail read path는 request-time body JSON thumbnail scan, `ContentJson`, `AllPropertiesJson`, `Assets`, `WorkVideos` lookup을 피한다.
- Public DTO들은 source-generated JSON metadata에 등록되어 있다. Evidence: `WoongBlogApiJsonSerializerContext`.
- Real runner는 target별 p95, payload bytes p95, receive p95, app elapsed, nginx timing, DB diagnostics를 수집할 수 있다. Evidence: `K6RealLoadTestRunner`, `RealLoadTestTargetMetrics`, `LoadTestDiagnosticsSampler`.

## Scan Execution Checklist
선택 hot-path 파일 기준 grep/rg scan 결과:

- `AsNoTracking(`: 19 hits.
- `EF.CompileAsyncQuery`: 2 hits.
- `SqlQuery<`: 2 hits.
- `CountAsync(`: 2 hits.
- `ToListAsync(`: 10 hits.
- `Include(`: 0 hits.
- `JsonSerializer.`: 3 hits.
- `JsonDocument.Parse`: 7 hits.
- `RegexOptions.Compiled`: 4 hits.
- Blocking wait pattern `.Result` / `.Wait(`: 0 hits.

Interpretation:

- Public read hot paths are already projection-based and no-tracking.
- No eager `Include` or sync-blocking wait pattern was found in selected public hot path files.
- Remaining DB command opportunities are not in the default page 1/no-search/read-detail mix; they are mostly search/page>1 or non-load-test paths.
- Remaining heavy cost is now more likely response size/serialization/network transfer and saturation behavior than extra DB roundtrips.

## User-Provided Load Signal Interpretation
사용자가 제공한 production-like 결과에서 중요한 신호는 `maxVUs`를 올렸을 때 처리량이 늘어난 게 아니라 대기열과 실패가 커졌다는 점이다.

### Spike 100 -> 1000 rps, maxVUs 100
- Requests: 13,944
- Throughput: 231.5 rps
- HTTP failed: 0
- Client p95: 592.1 ms
- ASP.NET app elapsed p95: 481.8 ms
- ThreadPool queue peak: 25
- DB connection open peak: 343.4 ms
- Active DB connections peak: 33

### Spike 100 -> 1000 rps, maxVUs 1000
- Requests: 13,780
- Throughput: 206.3 rps
- HTTP failed: 496, all 5xx
- Client p95: 2,736.4 ms
- ASP.NET app elapsed p95: 2,696.1 ms
- ThreadPool queue peak: 71
- DB connection open peak: 694.4 ms
- Active DB connections peak: 42
- Open connections peak: 100

Interpretation:

- `maxVUs=1000`은 서버 처리량을 올리는 설정이 아니라 runner가 더 많은 outstanding work를 만들 수 있게 하는 상한이다.
- 처리량은 231.5 rps에서 206.3 rps로 내려갔고, p95와 5xx가 폭증했다.
- 병목 신호는 app elapsed, ThreadPool queue, DB connection open latency에 같이 나타난다.
- 2 CPU / total 8 GiB 환경에서는 1000 rps spike 성공을 1차 목표로 두기보다, 실패 없는 sustained RPS ceiling과 saturation knee를 찾아야 한다.

## Already Improved And Expected Test Impact
### A1. Public list first-page window query and covering indexes
Expected impact:

- Work/Study list p95: lower for page 1/no-search.
- DB command count: 2 -> 1 for default list target.
- DB connection/open pressure: lower under list-heavy traffic.
- Does not directly improve large detail read payload cost.

### A2. Public content/thumbnail/cover read fields
Expected impact:

- Work/Study detail app elapsed: lower because request-time `ContentJson` parsing and asset/video fallback lookup are avoided.
- DB payload from table row: lower because admin JSON/properties are not selected.
- Correctness guarded by resolver-equivalence tests for thumbnail/cover behavior.
- Does not reduce the actual public body HTML/Markdown returned to the client.

### A3. Work public video read model
Expected impact:

- Work read with videos: DB command count 2 -> 1.
- DB command/open latency: lower under Work-read-heavy traffic.
- App elapsed p95: lower when DB pool/open wait contributes to latency.
- HTTP 5xx: can decrease if prior failure source was DB connection/open pressure.
- Payload P95: mostly unchanged unless video optional fields are also omitted.

### A4. Empty public video JSON fast path
Expected impact:

- Work read without videos: lower allocation by skipping `JsonSerializer.Deserialize` for `[]`.
- GC heap/Gen2: small improvement under high no-video Work detail RPS.
- DB metrics and payload size: no direct improvement.

## Remaining Prioritized Opportunities
### P1. Large detail body transfer and serialization cost
Current:

- Public detail already reads stored public body fields instead of admin JSON.
- However, if a real Work/Study read target has a 10-page text body or large HTML, the endpoint still sends that full body every request.

Candidate A:

- Add response compression for JSON/text responses at nginx or ASP.NET Core boundary. This is not cache.
- Measure on the real 2 CPU host because compression trades network bytes for CPU.

Candidate B:

- Add payload budget gates to Real Backend Test results: `responseBytesP95`, `receiveP95Ms`, `data_received`, app elapsed p95.
- Keep real targets and `pageSize=12`; do not hide workload with seed targets.

Candidate C, product/API-dependent:

- Split detail shell and body endpoint only if frontend/product can accept it. This changes API behavior and should not be done purely to win the current backend test.

Expected Real Backend Test impact:

- Payload/data received: lower if compression is effective.
- Receive P95: lower on external HTTPS when response size dominates.
- Client p95: lower if network transfer dominates.
- App elapsed p95: may stay similar or rise slightly because compression costs CPU.
- GC heap/time: may improve or worsen depending on compression implementation and buffer allocation.
- DB command/open latency: no direct improvement.

Risk:

- On shared 2 CPU, compression can move bottleneck to CPU. A/B test is mandatory.

### P2. Search and non-first-page public list queries
Current:

- Default page 1/no-search Work/Blog list is already 1 command.
- Search or page > 1 falls back to `CountAsync` plus page query, so 2 commands remain.

Candidate:

- Add compiled queries or window query variants for page > 1 and search paths.
- Run `EXPLAIN ANALYZE` on production-like row counts to confirm trigram index use.
- Consider `hasNextPage`/cursor pagination only if exact `totalItems` semantics can change.

Expected Real Backend Test impact:

- Current default Real Backend Test: little/no direct impact unless targets include search/page > 1.
- Search/page > 1 list p95: lower.
- DB command/open p95: lower under search/list traffic.
- Throughput: improves only if those paths are in the mix.

Risk:

- Removing exact total count would alter API contract. Do not do this silently.

### P3. Public DTO serialization fast-path
Current:

- Public DTOs are registered in `WoongBlogApiJsonSerializerContext` with source-generated metadata mode.
- Public detail still materializes object graphs and large strings before JSON write.

Candidate:

- Benchmark source-generation `Serialization` mode or explicit endpoint-level `Results.Json(..., JsonTypeInfo)` for public hot DTOs.
- Keep reflection/admin fallback safe.
- Continue omitting fields only when frontend contract treats missing and null as equivalent.

Expected Real Backend Test impact:

- App elapsed p95: lower if serialization is CPU/allocation bound.
- GC heap/Gen2: lower under high RPS if allocation reduction is material.
- CPU-bound throughput: modest improvement.
- Payload P95: unchanged unless DTO fields are removed/omitted.
- DB metrics: no direct improvement.

Risk:

- ASP.NET execution path must actually use the intended `JsonTypeInfo`; validate with benchmark/test, not assumption.

### P4. Observability completeness and deployment verification
Current:

- Code has `X-App-Elapsed-Ms`, nginx timing headers, target payload/receive metrics, DB command/open diagnostics, and run-scoped samples.
- Earlier screenshots still had unavailable values, which can come from stale image/config, missing nginx route header, or direct backend path bypassing nginx.

Candidate:

- Add an operational smoke script/report that checks production container/image exposes:
  - `LoadTesting__BaseUrl=https://woonglab.com` when measuring HTTPS path.
  - nginx config includes `X-Nginx-Request-Time` and `X-Nginx-Upstream-Response-Time`.
  - backend response includes `X-App-Elapsed-Ms`.
  - Real Backend target summary shows Payload P95 and Receive P95.
  - DB command latency samples appear during a run.

Expected Real Backend Test impact:

- No direct raw latency improvement.
- Much better root cause separation: network/nginx/app/db/payload.
- Prevents chasing backend code when the problem is stale deployment/config.

Risk:

- Low. This is measurement reliability work.

### P5. Write-time parsing cost cleanup
Current:

- Public read path no longer parses admin content JSON.
- Write paths still parse `ContentJson` multiple times through `AdminContentJson` and regex helpers.

Candidate:

- Consolidate write-time parsing so body fields, excerpt text, search text, and social share message are extracted with fewer parses per save.
- Convert static regexes to generated regex where appropriate.

Expected Real Backend Test impact:

- Current read-only Real Backend Test: almost none.
- Admin/import/batch write paths: lower CPU/allocation.

Risk:

- Correctness-sensitive for content rendering/search; requires equivalence tests.

## What Not To Do
- Do not use cache as the next fix. User explicitly excluded cache except as last resort.
- Do not reduce `pageSize=12`; that hides the actual list workload.
- Do not force seed targets; that hides real content distribution.
- Do not raise `maxVUs` expecting more server capacity. It only permits more outstanding runner work and can worsen p95/5xx after saturation.
- Do not remove public detail body fields without product/API change.

## Recommended Next Slice
Next implementation should be P1 measurement-first, not blind code change.

Recommended sequence:

1. Run controlled real backend tests at sustained levels, e.g. 200/300/400/500 rps constant-arrival-rate, maxVUs sized only high enough to avoid runner starvation.
2. Compare target-level p95, responseBytesP95, receiveP95Ms, app elapsed p95, DB command/open p95, ThreadPool queue, and 5xx.
3. If Work/Study detail read has high `responseBytesP95` and `receiveP95Ms`, test compression A/B.
4. If app elapsed remains high while receive is small and DB is low, pursue serialization fast-path.
5. If DB open/command latency rises again, inspect pool/connection config and exact target causing pressure.

## Validations Performed
- Code scan: Work/Blog public query stores, Home query store, public DTOs, JSON context, DB bootstrap/indexes, nginx timing headers, k6 real runner metrics.
- Pattern scan: no `Include(` or blocking wait patterns found in selected public hot path files.
- PR #102 CI: all checks passed before merge.
- PR #102 merge: merged to `dev` as commit `6a18e20`.
- Targeted PostgreSQL contracts passed 5/5:
  - `PublicWorkDetailWithVideos_UsesSinglePostgresCommand_AndStoredPublicColumnsOnly`
  - `PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand_AndResolverEquivalentStoredThumbnail`
  - `PublicBlogDetail_UsesSinglePostgresCommand`
  - `PublicWorkFirstPage_UsesSinglePostgresCommand_ForNoSearchList`
  - `PublicBlogFirstPage_UsesSinglePostgresCommand_ForNoSearchList`

## Risks And Yellow Flags
- The large-detail bottleneck may be dominated by response size, not DB. Further DB query micro-optimization may not move p95 much.
- Compression can help HTTPS transfer but can hurt CPU on a 2 CPU shared host.
- A single `public-api-spike` run is not enough to set capacity. Use constant runs to locate the saturation knee.
- `main` promotion/GHCR publish for latest `dev` needs separate verification if production deployment must be updated immediately.

## Final Recommendation
Do not change the test realism. Keep real targets and `pageSize=12`. The backend structure has already removed the obvious DB roundtrip/read-model issues for the current four-target mix. The next meaningful improvement should be selected from measured attribution:

- If payload/receive dominates: compression or API body split design.
- If app elapsed/GC dominates: serialization fast-path and DTO shape work.
- If DB open/command dominates again: pool/connection pressure and exact target query inspection.
