# Real Load Run Diagnostics Persistence - 2026-05-06

## Goal
Make completed Real Backend Test results retain runtime and DB diagnostics so the next backend performance improvement can be chosen from evidence instead of guessing.

## What Changed
- Added `LoadTestDiagnosticsSampler` as a reusable diagnostics capture service for process, GC, ThreadPool, DB probe, DB command latency, DB connection-open latency, slow-query, timeout, error, and PostgreSQL connection-count data.
- Refactored `GET /api/admin/load-test/diagnostics` to use the shared sampler instead of maintaining a second copy of diagnostics capture logic.
- Added diagnostics snapshots to `RealLoadTestMetricPoint` and exposed run-level `diagnostics` in `RealLoadTestMetricsResponse`.
- Updated fake and k6 real load runners so each persisted metric point can include the diagnostics snapshot captured for that run.
- Updated the dashboard model to extract persisted diagnostics from completed Real Backend metrics and use them when no live diagnostics samples are present.
- Added backend integration coverage and frontend unit coverage for persisted diagnostics in Real Backend metrics.

## What Was Intentionally Not Changed
- No cache was added.
- Real Backend Test target semantics were not changed.
- Public list targets remain `pageSize=12`.
- No seeded Work/Blog priority was introduced.
- No public API query/read DTO behavior was changed in this slice.
- No claim is made that this alone reduces p95 latency or raises max sustainable rps.

## Goal Verification
- Completed Real Backend metrics now include a top-level `diagnostics` array.
- Metric points can include their own `diagnostics` payload.
- The dashboard can display DB command/open latency from the completed run payload when live polling samples are unavailable.
- A runtime k6 smoke confirmed the new payload is present after a completed real backend run.

## Validation Performed
- RED: focused backend integration test failed before implementation because `metricsPayload.Diagnostics` was absent.
- RED: focused frontend unit test failed before implementation because persisted diagnostics were not extracted.
- PASS: focused backend integration test for fake-runner Real Backend metrics passed 1/1.
- PASS: diagnostics/health focused integration tests passed 2/2.
- PASS: RealLoadTestRunner component test passed 1/1.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed backend solution: Contract 1 skipped, Component 122/122, Unit 56/56, Architecture 35/35, Integration 223/223. Existing AWSSDK.Core NU1901 warnings remain.
- PASS: `npm test -- --run src/test/load-test-dashboard.test.ts` passed 28/28.
- PASS: `npm run typecheck`.
- PASS: `npm run lint` completed with 0 errors and 6 pre-existing warnings.
- PASS: `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` rebuilt and restarted the dev compose stack.
- PASS: nginx and backend health endpoints returned 200 JSON.
- PASS: backend container env confirmed `LoadTesting__BaseUrl=http://nginx` and Npgsql `Maximum Pool Size=40;Minimum Pool Size=0`.
- PASS: focused Playwright `tests/admin-load-test-dashboard.spec.ts` passed 2/2 against the rebuilt compose stack.
- PASS: runtime Real Backend k6 smoke completed: run `20260506-100717-public-api-rps-5c1318b5`, 26 requests, p95 8.4ms, diagnostics samples 1, metric points with diagnostics 1, DB status `available`, DB command samples 192.
- PASS: audit JSON parsed successfully.

## Runtime Smoke Artifacts
- `backend/reports/real-load-run-diagnostics-2026-05-06/loadtest/runtime-start.json`
- `backend/reports/real-load-run-diagnostics-2026-05-06/loadtest/runtime-status.json`
- `backend/reports/real-load-run-diagnostics-2026-05-06/loadtest/runtime-metrics.json`

## Expected Backend Test Impact
- `DB command P95/P99` and DB command sample count should no longer be lost after a Real Backend run completes, as long as the diagnostics collector has samples.
- `DB connection open P95/P99` should be attributable to the run rather than only to the latest live polling window.
- Future backend performance changes can be judged by whether app elapsed, DB command latency, DB connection-open latency, ThreadPool queue, GC heap, or client p95 moves.
- This is measurement/attribution work. It should not be counted as a throughput optimization by itself.

## Risks And Yellow Flags
- k6 currently appends one summary metric point at run completion, so k6 diagnostics are completion-time snapshots, not high-resolution timeline samples.
- Fake runner stores diagnostics on every tick, which is useful for tests but more frequent than k6 summary persistence.
- DB command sample count depends on the diagnostics interceptor being active in the running backend.
- Dashboard still prefers live diagnostics while a run is active; persisted diagnostics are the fallback for completed run readback.

## Final Recommendation
Use the now-persisted run diagnostics in the next load-test comparison. If spike p95 remains high, choose the next structural fix based on which metric moves: DB command/open latency means DB/pool/query shape, high app elapsed with normal DB means serialization/payload/runtime, and ThreadPool/GC pressure means allocation/concurrency tuning.
