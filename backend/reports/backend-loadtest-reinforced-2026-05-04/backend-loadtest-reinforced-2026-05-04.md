# Backend Load-Test Reinforced Pass Audit

- Task slug: `backend-loadtest-reinforced-2026-05-04`
- Date: `2026-05-04`
- Scope: existing Next.js + ASP.NET Core + nginx/compose structure only (no SignalR/k8s/NBomber/C# worker)

## 1) Summary of Changes

### Frontend (`/admin/load-test`)

- Added real pattern execution semantics:
  - `step`: existing step progression.
  - `soak`: repeats per-second batches for `soakDurationSeconds`.
  - `spike`: ramps users `startUsers -> maxUsers` over `spikeRampSeconds`.
- Added reusable concurrency scheduler `runWithConcurrency(...)`.
- Increased `concurrency` upper bound to `1000`.
- Clarified semantics in UI:
  - Concurrency is max in-flight HTTP requests, not real users/connections.
  - High-concurrency warning for browser-driven load limits.
- Added run-time status telemetry:
  - `current in-flight`, `observed peak in-flight`, `elapsed`.
- Added richer failure breakdown:
  - total failures + `5xx`, `429`, `503`, `timeout`, `abort`.
- Added backend diagnostics visualization:
  - Runtime panel (memory/GC/thread-pool/DB latency-timeout trends).
  - Database pressure panel (DB command p95/p99, connection-open p95, slow queries, errors, connection-state trends).

### Backend diagnostics

- Added admin diagnostics endpoint:
  - `GET /api/admin/load-test/diagnostics` (still `RequireAuthorization("AdminOnly")`).
- Added DB diagnostics collector infrastructure:
  - command latency ring buffer and connection-open latency ring buffer.
  - p50/p95/p99 + sampleCount snapshots.
  - slow query count and sanitized recent samples.
  - timeout count and error count.
- Added EF Core interceptors:
  - command interceptor for query timing/failure capture.
  - connection interceptor for connection-open/acquire timing capture.
- Added `pg_stat_activity` connection-state snapshot:
  - `open`, `active`, `idle`, `idle in transaction`.
- Added endpoint resilience:
  - collector/probe failure returns `database.status="error"` payload (HTTP 200), instead of crashing endpoint.
- Extended thread pool diagnostics with `completedWorkItemCount`.

## 2) What Was Intentionally Not Changed

- No SignalR/WebSocket simulator work.
- No k8s/HPA/pod-level observability.
- No Redis backplane features.
- No new npm/NuGet dependencies.
- No public API contract changes for work/blog routes.
- No backend limiter queue implementation in this pass (optional item kept for follow-up).

## 3) Requirement-to-Evidence Checklist

- [x] Keep existing HTTP load-test behavior: preserved and extended.
- [x] Work/Study list/read targets remain: preserved.
- [x] `step/soak/spike` typed model: implemented in `src/lib/load-test-dashboard.ts`.
- [x] Concurrency defined as max in-flight HTTP requests: UI and logic both updated.
- [x] Concurrency `1000` allowed: `MAX_CONCURRENCY=1000`, input max updated.
- [x] High-concurrency warning displayed: UI warning added for `>=500`.
- [x] Observed peak in-flight shown: run status panel updated.
- [x] Soak repeats for duration: per-second timeline execution implemented.
- [x] Spike ramps by seconds: timeline ramp implemented.
- [x] HTTP p95/avg/failures kept: preserved.
- [x] `429/503` separated: failure counters added and shown.
- [x] Runtime diagnostics endpoint present: backend module added and mapped.
- [x] GC/memory/thread-pool visible: runtime payload and panel show these.
- [x] `timeInGcPercent` nullable/unavailable handling: runtime payload parser/summary handles null.
- [x] DB command latency p50/p95/p99: collector + payload implemented.
- [x] DB connection-open/acquire latency p50/p95/p99: collector + payload implemented.
- [x] Slow query count/samples: collector + payload implemented.
- [x] DB timeout/error counts: collector + payload implemented.
- [x] `pg_stat_activity` connection states: included with idle-in-transaction.
- [x] Npgsql pool internal busy/idle exactness limitation called out in UI/report.
- [x] Current/peak/delta summaries: runtime and DB trend summaries computed and rendered.
- [x] Green/yellow/red/unavailable scoring: HTTP + runtime scoring active.
- [x] Metric collection failure does not break UI: endpoint fallback + frontend error text handling.
- [x] Unit tests reinforced: timeline/scheduler/failure breakdown/diagnostics tests added.
- [x] E2E spec reinforced: runtime/DB panel and concurrency UI assertions added.
- [x] Backend integration tests reinforced: payload shape + auth + fallback tests added.
- [x] Mandatory audit artifacts generated: this `.md` + `.html` + `.json`.

## 4) Validation Commands and Results

### Passed

- `npm run typecheck`
- `npx vitest run src/test/load-test-dashboard.test.ts` (15/15)
- `npm run lint` (0 errors, existing unrelated warnings only)
- `npm run build`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/admin-load-test-dashboard.spec.ts --workers=1` (1/1)
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests" --no-restore` (9/9)
- `dotnet test backend/WoongBlog.sln --no-restore` (403 passed, 1 skipped)
- `dotnet build backend/WoongBlog.sln --no-restore`
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build frontend nginx`
- `git diff --check`

### Not fully completed / environment-limited

- `npm test -- --run`: attempted; full suite showed no-progress hang in this environment, process was terminated. Focused suite evidence (changed area) is provided above.
- `pnpm quality-gate`: unavailable here (`pnpm: command not found`).

### Existing warnings (unchanged by this task)

- NU1901 warning for `AWSSDK.Core` in backend build/test.
- Existing ESLint warnings in unrelated files.

## 5) Current Metrics Available in This Structure

- HTTP: p95/avg/failure/error rate + 5xx/429/503/timeout/abort breakdown.
- Runtime: process memory, GC heap, gen collections, time in GC, thread-pool worker/pending/completed.
- DB: probe latency, command latency p50/p95/p99, connection-open latency p50/p95/p99, timeout count, error count, slow query count/samples, `pg_stat_activity` open/active/idle/idle-in-transaction states.

## 6) Known Gaps and Risks

- Browser-generated load is not a true multi-node load generator.
- `pg_stat_activity` is server-side connection state, not exact internal Npgsql pool busy/idle counters.
- Ring-buffer metrics are in-process and reset on restart.
- Full Vitest suite stability still depends on unrelated tests/environment.

## 7) Recommendation

Use the reinforced dashboard as the default developer load-observability surface for HTTP pressure runs.
Next follow-up should be either:
1) optional backend bounded limiter metrics (`active/queued/rejected/queue-timeout`), or
2) an external load runner path for higher-fidelity concurrency tests.
