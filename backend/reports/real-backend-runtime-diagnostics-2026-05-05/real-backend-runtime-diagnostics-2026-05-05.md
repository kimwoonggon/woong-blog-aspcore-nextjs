# Real Backend Runtime Diagnostics Audit - 2026-05-05

## Objective

Real Backend/k6 load tests must track the same backend pressure signals as the browser load test: memory, GC, ThreadPool, and database pressure. Scenario-specific Real Backend options must remain available and verified.

## Changed

- `src/components/admin/LoadTestDashboard.tsx`
  - Real Backend `starting`, `running`, and `stopping` phases now activate diagnostics polling.
  - Real Backend start clears prior diagnostics samples so runtime and database panels describe the current run.
- `tests/admin-load-test-dashboard.spec.ts`
  - Real Backend E2E now verifies diagnostics polling, GC/ThreadPool panel updates, DB pressure panel updates, and component timing DB p95 during a Real Backend run.
- `todolist-2026-05-05.md`
  - Added the Real Backend Runtime Diagnostics checklist and validation evidence.

## Intentionally Not Changed

- No backend diagnostics endpoint contract change was needed. `/api/admin/load-test/diagnostics` already exposes process, GC, ThreadPool, and DB pressure payloads.
- No k6 summary contract change was needed for this slice. k6 still reports request, HTTP, latency, target, nginx, and app elapsed metrics through the existing real-run endpoints.
- No unrelated full E2E flake fixes or promotion workflow changes were included in this correction.

## Checklist

- GC/runtime tracking: covered by Real Backend-triggered diagnostics polling and E2E assertions for `Time in GC`.
- ThreadPool tracking: covered by E2E assertions for `ThreadPool queue`.
- Memory tracking: runtime panel remains driven by diagnostics samples and is activated during Real Backend runs.
- DB pressure tracking: covered by E2E assertions for `DB command P95`, `Idle in transaction`, and Real Backend component `db command p95`.
- Detailed Real Backend options: existing scenario-specific controls remain covered by the E2E stress scenario assertions and payload verification.
- Actual k6 path: live Docker UI smoke started and completed a real k6 run, with diagnostics endpoint responses collected during that run.

## Validation

- `npx vitest run src/test/load-test-dashboard.test.ts --pool=threads --maxWorkers=1`: passed 24/24.
- `npm run typecheck`: passed.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests.RealLoadTestControlPlane" --no-restore`: passed 4/4.
- `npm run lint`: passed with six existing unrelated warnings.
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build backend frontend nginx`: passed.
- `curl -fsS http://127.0.0.1:3000/api/health`: passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1`: passed 2/2.
- Live Docker UI smoke: `public-api-rps`, `k6`, `rate=1`, `durationSeconds=3`, `maxVUs=2` completed; diagnostics endpoint returned HTTP 200 five times; runtime/database/target panels displayed diagnostics and real target metrics.

## Risks And Follow-Up

- `npm run lint` still reports six pre-existing warnings outside this task.
- The diagnostics samples are UI-held just like the existing browser load test path; this change does not persist runtime diagnostics into the real-run report store.
- Full E2E and CI promotion stabilization are separate pending items already tracked in `todolist-2026-05-05.md`.

## Recommendation

Treat the Real Backend runtime diagnostics gap as closed. Continue with the remaining post-promotion CI stabilization items before any new dev/main promotion.
