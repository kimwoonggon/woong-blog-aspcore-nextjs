# Backend Performance Opportunity Scan - 2026-05-06

## Scope

Audit-only scan for public backend load-test hot paths. No production or test code was changed.

Targets considered:
- `/api/public/works?page=1&pageSize=12`
- `/api/public/works/{slug}`
- `/api/public/blogs?page=1&pageSize=12`
- `/api/public/blogs/{slug}`

User goal:
- Identify where backend performance can still be improved.
- Map each improvement candidate to Real Backend Test metrics that should move.
- Do not hide performance issues by switching to seeded/light targets or by reducing `pageSize=12`.
- Do not use cache as the first solution.

## Current State Verified

### Already Improved / Not The Main Remaining Gap

- Public work detail uses `AsNoTracking`, compiled EF query, and projects stored public fields instead of reading full `ContentJson`.
- Public blog detail uses `AsNoTracking`, compiled EF query, and projects stored public fields instead of reading full `ContentJson`.
- Public first-page list endpoints use one PostgreSQL window query, not count query plus page query, when page is 1 and no search query is present.
- Public list fields are projected DTO/read-model fields, not full entity materialization.
- Source-generated JSON metadata is registered for public DTOs.
- Compose defaults now derive `ConnectionStrings__Postgres` with `Maximum Pool Size=${POSTGRES_MAX_POOL_SIZE:-40}`.

### Remaining Hot Spots

1. DB connection/concurrency pressure under spike load.
2. Per-request DbContext allocation and EF service overhead under high RPS.
3. Large detail response serialization and transfer cost, especially long blog bodies and work pages with video payloads.
4. Public list exact count semantics: `COUNT(*) OVER()` keeps one roundtrip but still counts all published rows per list request.
5. Work detail with videos is necessarily two DB commands today: one work detail query plus one video query.
6. Diagnostics collection adds a small per-DB-command synchronized write during load tests.

## Findings And Expected Backend Test Impact

### P0 - Connection Pool / DB Pressure Tuning

Evidence:
- Production compose defines `POSTGRES_MAX_POOL_SIZE`, but real run output previously showed open connections reaching 100. That suggests deployed runtime may not have picked up the pool limit, or the actual `.env.prod`/compose stack was not updated/recreated with the derived connection string.
- In the failed spike run, DB connection open P95 peaked around hundreds of ms and active connections peaked high while ThreadPool queue also spiked.

Potential change:
- Verify deployed backend `ConnectionStrings__Postgres` actually contains `Maximum Pool Size`.
- For a 2 CPU / 8 GiB host shared by nginx, frontend, backend, and DB, test lower values such as 16, 24, and 32 rather than 100.
- Keep enough spare DB connections for diagnostics/admin access.

Expected Real Backend Test improvement:
- `Database pressure > Open connections`: should cap below the configured maximum instead of approaching 100.
- `DB connection open P95`: should stop spiking into hundreds of ms if pool churn is controlled.
- `DB latency peak`: should become less erratic during spike.
- `ThreadPool queue`: should reduce if fewer requests are blocked deep in DB connection/open paths.
- `5xx`: may drop if failures were pool/DB saturation. If offered RPS still exceeds capacity, latency may rise instead of producing 5xx.

Risk:
- Too-low pool size can reduce throughput and raise request latency. This must be tuned by repeated 100 -> 300 -> 500 RPS runs, not guessed.

### P1 - Use DbContext Pooling For Hot API Requests

Evidence:
- Current persistence registration uses `AddDbContext<WoongBlogDbContext>` rather than `AddDbContextPool`.
- The public endpoints are short-lived read-only requests with no per-context mutable service dependency.
- Under high RPS, reducing DbContext construction/service setup should reduce allocation and CPU overhead.

Potential change:
- Switch relational registration to `AddDbContextPool<WoongBlogDbContext>` with a bounded pool size.
- Keep interceptors singleton-safe and validate in integration tests.

Expected Real Backend Test improvement:
- `ASP.NET app elapsed p95`: modest reduction.
- `GC heap`, `Gen2 GC`, `Memory peak`: modest reduction.
- `ThreadPool completed` per second should become more stable at the same RPS.
- This will not reduce DB command count or response bytes.

Risk:
- DbContext pooling requires no request-specific mutable state inside the context. Current `WoongBlogDbContext` looks compatible, but tests must cover save-change synchronizer behavior and diagnostics interceptors after pooling.

### P1 - Detail Payload Reduction By Public DTO Contract Audit

Evidence:
- Public detail no longer returns admin `ContentJson`, but it still returns full public content body by design.
- Work detail also returns video metadata fields used by the frontend player: source type/key, playback URL, mime type, dimensions, duration, preview URLs, sort order.
- Large article bodies and video-related payloads directly affect JSON serialization and response transfer cost.

Potential change:
- Audit frontend-visible fields and split public/admin DTOs further if any public fields are only admin/editor concerns.
- Do not remove fields needed by `WorkVideoPlayer`, metadata, or detail rendering.
- If a field is public but not needed for first paint, consider separate endpoint only after validating UX/API contract.

Expected Real Backend Test improvement:
- `Latency p95/p99`, especially Work read and Study read target p95.
- `target response bytes p95` and `target receiving p95` if surfaced in UI/report.
- `ASP.NET app elapsed p95` may improve if serialization dominates.

Risk:
- Removing a field without frontend/API contract tests can break public rendering. This should be TDD-first with pact/client tests.

### P2 - Public List Count Strategy

Evidence:
- First-page list query uses `COUNT(*) OVER()` to keep list at one DB command.
- This is good for roundtrip count but still computes exact total count for every list request.
- UI uses `totalPages`, so exact count is currently part of the public contract.

Potential change:
- Keep `pageSize=12`, but add an alternate lightweight contract only if UI can accept `hasMore` or approximate total.
- Otherwise keep current query; it is already better than two DB commands for first page.

Expected Real Backend Test improvement:
- Work list and Study list p95 if published row count grows large.
- `DB command P95` and `ASP.NET app elapsed p95` for list targets.

Risk:
- API contract/UI pagination changes are broader than backend-only tuning.

### P2 - Work Detail With Videos Query Shape

Evidence:
- Work detail without videos is one DB command.
- Work detail with videos is two commands: detail row plus ordered video rows.
- There is already an index on `(WorkId, SortOrder)` for `WorkVideos`.

Potential change:
- Keep two commands unless measured video query latency is material.
- Add a contract test that work detail with videos stays at two commands and does not read `ContentJson`.
- Avoid joining all videos into the work query unless evidence shows the second roundtrip is the bottleneck.

Expected Real Backend Test improvement:
- Usually small unless selected work has many videos or remote playback URL construction becomes expensive.
- Most impact would show on Work read p95, not list or blog read.

Risk:
- A single join can duplicate large work body fields per video and make performance worse.

### P3 - Response Compression / Nginx Compression

Evidence:
- Current nginx config does not enable gzip/brotli in the inspected files.
- ASP.NET response compression is not configured.

Potential change:
- Consider gzip for JSON/HTML responses after backend structural fixes.
- Treat this as infra/payload transfer optimization, not the first backend structural fix.

Expected Real Backend Test improvement:
- `target receiving p95` and client-side duration for large detail JSON.
- `ASP.NET app elapsed` likely unchanged if compression is handled by nginx after upstream response.

Risk:
- CPU cost on a 2 CPU shared host can offset network gains. Benchmark with compression on/off.

## Non-Recommendations

- Do not add covering indexes that include large public detail body columns. Slug is already unique, so detail lookup is already selective; indexing large content fields would bloat indexes and can hurt memory/write performance.
- Do not reduce load-test `pageSize=12` to make results look better.
- Do not switch read targets to seeded/light content as a performance fix.
- Do not use cache as the first response to the observed backend pressure.

## Suggested Next Implementation Slice

Recommended first code/config slice:

1. Verify deployed `ConnectionStrings__Postgres` and `POSTGRES_MAX_POOL_SIZE` are actually active.
2. Add a small production-safe diagnostic/status output or deployment check if verification is currently manual.
3. Switch backend EF registration to `AddDbContextPool` behind tests if context pooling is compatible.
4. Run the same Real Backend Test matrix with realistic targets and `pageSize=12`:
   - baseline 100 RPS, 60s, maxVUs 100
   - spike 100 -> 500 -> 100, 60s, maxVUs 500
   - spike 100 -> 1000 -> 100, 60s, maxVUs 1000 only after the lower runs are green

Primary pass/fail metrics:
- HTTP failed = 0 for 100 and 500 target runs.
- p95 below current baseline for Work read and Study read.
- DB open connections capped below configured pool max plus non-backend connections.
- ThreadPool queue stays near 0 at 100 RPS and does not explode in spike.
- DB connection open P95 remains near 0 ms or low single-digit ms under stable phases.

## Validation Performed

Commands/scans run:
- `git status --short --branch --untracked-files=no`
- `npx -y skills find "dotnet ef core performance query projection"`
- Source scans for `ContentJson`, `JsonDocument.Parse`, `JsonSerializer`, `Include`, `ToListAsync`, `CountAsync`, `EF.CompileAsyncQuery`, `SqlQuery`, `AsNoTracking`, `WorkVideos`, public media/content fields.
- Inspected public hot path files:
  - `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkQueryStore.cs`
  - `backend/src/WoongBlog.Infrastructure/Modules/Content/Blogs/Persistence/BlogQueryStore.cs`
  - `backend/src/WoongBlog.Infrastructure/Modules/Composition/Persistence/HomeQueryStore.cs`
  - `backend/src/WoongBlog.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`
  - `backend/src/WoongBlog.Infrastructure/Persistence/DatabaseBootstrapper.cs`
  - `backend/src/WoongBlog.Api/Common/Json/WoongBlogApiJsonSerializerContext.cs`
  - `nginx/nginx.conf`, `nginx/prod.conf`, `nginx/default.conf`
  - frontend public detail/API client files for visible DTO usage

No build/test command was run because this was an audit-only scan and no code was changed.

## Final Recommendation

Proceed with a measured P0/P1 slice: first verify and enforce deployed DB pool sizing, then evaluate `AddDbContextPool` with tests. This targets the failed spike symptoms directly: connection pressure, ThreadPool queue, GC/allocations, and app elapsed p95. Payload/DTO reduction should be second, because it carries more API/frontend contract risk.
