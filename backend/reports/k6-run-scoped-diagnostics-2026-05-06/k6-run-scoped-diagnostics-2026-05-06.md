# K6 Run-Scoped Diagnostics Sampling Audit - 2026-05-06

## Goal

Improve Real Backend Test observability so completed k6 runs retain runtime and database pressure samples captured during the load window, not only the final completion sample. This supports backend performance analysis under realistic targets without using cache shortcuts, seeded target priority, or reducing list `pageSize=12`.

## Changed

- Added 1-second run-scoped diagnostics sampling while the k6 process is running.
- Persisted each run-scoped diagnostics snapshot through the existing real load report store as a metric with diagnostics attached.
- Awaited the background diagnostics task on normal completion, cancellation, and failure paths so expected cancellation does not leak unobserved task exceptions.
- Added component coverage with a fake k6 executable proving more than the final diagnostics sample is persisted while the process is still running.
- Recorded live smoke artifacts under `backend/reports/k6-run-scoped-diagnostics-2026-05-06/loadtest/`.

## Intentionally Not Changed

- No public endpoint response DTO changes.
- No public Work/Blog target selection shortcuts.
- No seeded slug priority.
- No list `pageSize=12` reduction.
- No cache policy changes.
- No database schema or query-shape changes.
- No k6 scenario semantics change for RPS, peak RPS, duration, or max VUs.
- No direct latency optimization in this slice.

## Goal Verification

- The RED component test failed before implementation because k6 only persisted the final diagnostics sample.
- The same test passed after implementation with at least two diagnostics samples captured while fake k6 was still running.
- Live Real Backend smoke completed with 10 diagnostics entries and 10 metric diagnostics entries in the completed metrics payload.
- Smoke targets used real public paths, kept list `pageSize=12`, and did not use `seeded-work` or `seeded-blog`.

## Validation

- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~RealLoadTestRunnerComponentTests.K6Runner_PersistsRunScopedDiagnosticsWhileProcessIsRunning" --no-restore --logger "console;verbosity=minimal"` failed before implementation, then passed after implementation.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~RealLoadTestRunnerComponentTests" --no-restore --logger "console;verbosity=minimal"` passed 3/3.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~RealLoadTestControlPlane_StartStatusMetricsAndStop_HappyPath_WhenRealRunnerDisabled_ForcesFakeRunner" --no-restore --logger "console;verbosity=minimal"` passed 1/1.
- PASS: `npm test -- --run src/test/load-test-dashboard.test.ts` passed 28/28.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed. Contract 1 skipped, Component 124, Unit 56, Architecture 35, Integration 224. Existing AWSSDK.Core NU1901 warning remains.
- PASS: `npm run typecheck`.
- PASS: `npm run lint` completed with 0 errors and 6 existing warnings.
- PASS: `git diff --check`.
- PASS: `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` rebuilt and restarted compose.
- PASS: nginx `http://127.0.0.1:3000/api/health` and backend direct `http://127.0.0.1:18080/api/health` returned 200.
- PASS: backend container confirmed `LoadTesting__BaseUrl=http://nginx`.
- PASS: live Real Backend smoke `20260506-130939-public-api-rps-c8aaa852` completed: 10rps, 8s, maxVUs 20, 81 requests, 0 failures, p95 9.8ms, p99 64.1ms, 10 diagnostics entries.

## Live Smoke Targets

- Work list: `/api/public/works?page=1&pageSize=12`
- Work read: `/api/public/works/real-load-current-heavy-work-20260506`
- Study list: `/api/public/blogs?page=1&pageSize=12`
- Study read: `/api/public/blogs/real-load-current-heavy-study-20260506`

## Risks And Yellow Flags

- k6 still does not stream request counters while running; run-scoped samples mainly improve runtime/DB/ThreadPool/GC visibility until the final k6 summary is available.
- The diagnostics sampler interval adds one backend diagnostics capture and one report append per second during k6 runs. This is low overhead for admin-triggered tests, but should remain scoped to real load tests.
- Dev compose reports host-visible `processorCount=8`; production capacity assessment still needs cgroup-aware CPU/memory confirmation on the 2 CPU / 8 GiB shared server.
- Existing AWSSDK.Core NU1901 warning remains unrelated to this change.

## Final Recommendation

Merge this observability slice first. Then run a production-like spike again and use the retained per-second diagnostics to decide whether the next structural backend improvement should target DB pool/open latency, detail query/body materialization, JSON serialization/payload size, or ThreadPool queueing.
