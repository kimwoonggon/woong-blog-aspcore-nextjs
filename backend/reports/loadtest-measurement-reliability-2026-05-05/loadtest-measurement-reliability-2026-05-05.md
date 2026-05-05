# Real Backend Measurement Reliability Audit - 2026-05-05

## Changed
- Added DB diagnostic error count to runtime/DB health red thresholding.
- Added DB timing availability tracking so missing DB command/open latency samples render as `unavailable` instead of `0 ms`.
- Updated k6 script generation to export `p(99)` via `summaryTrendStats` and add conservative `http_req_failed` and `http_req_duration` thresholds.
- Added nginx upstream timing alias header `X-Nginx-Upstream-Response-Time` across dev/local/prod nginx configs.
- Added k6 upstream timing fallback: numeric nginx upstream headers are preferred, otherwise `response.timings.waiting` is recorded with source `runner.http_waiting_fallback`.
- Updated dashboard UI to show the upstream timing source when a source is available.
- Added behavior tests for DB errors, DB timing availability, k6 p99/threshold/upstream script contents, and UI Runtime red rendering for DB errors.

## Intentionally Not Changed
- Did not change the Docker dev default runner base URL from `http://nginx`; HTTPS browser access and backend-internal k6 target selection remain separate concerns.
- Did not claim nginx `$upstream_response_time` response headers are always exact; live curl showed the header can be `-`, so the fallback is explicitly labeled.
- Did not change public API query implementations or database pool sizing.
- Did not remove existing lint warnings unrelated to load-test reliability.

## Goal Verification
- DB errors are no longer green: `dbErrorCount.current >= 1` returns runtime health `red`.
- True p99 is now collected by k6 summary export: live k6 smoke showed p95 `9.6 ms` and p99 `13.4 ms`.
- nginx upstream no longer stays silently unavailable for this environment: live k6 smoke produced nginx upstream p95 `9.3 ms` with source `runner.http_waiting_fallback`.
- Official-doc research was applied:
  - k6 `summaryTrendStats` supports adding `p(99)` to summary output.
  - k6 thresholds are pass/fail automation criteria.
  - nginx upstream timing variables are measured in seconds with millisecond resolution and may have multiple values or `-`.
  - .NET runtime counters include GC and ThreadPool counters relevant to load-test diagnostics.

## Validations
- `npx vitest run src/test/load-test-dashboard.test.ts --pool=threads --maxWorkers=1`: passed 26/26.
- `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~RealLoadTestRunnerComponentTests" --no-restore`: passed 1/1.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests.RealLoadTestControlPlane" --no-restore`: passed 4/4.
- `npm run typecheck`: passed.
- `npm run lint`: passed with six existing unrelated warnings.
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build backend frontend nginx`: build/start passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1`: passed 2/2.
- Live Real Backend k6 smoke `20260505-073055-public-api-rps-46e65dd8`: completed, requests 26, failed 0, p95 9.6 ms, p99 13.4 ms, nginx upstream p95 9.3 ms, source `runner.http_waiting_fallback`.
- `dotnet test backend/WoongBlog.sln --no-restore`: passed; contract provider verification skipped by suite policy, component 115, unit 56, architecture 35, integration 202.
- `npm test -- --run`: passed 82 files and 588 tests.

## Risks And Follow-Ups
- nginx `$upstream_response_time` is still not reliably available as a response header in this local nginx setup; the fallback is useful but is not the same as nginx log-phase upstream timing.
- Exact Npgsql pool busy/idle counts remain unavailable; current DB connection counts are still estimated from `pg_stat_activity`.
- k6 thresholds can intentionally fail a run if latency/error budgets are exceeded; this is desired for automation but changes failure semantics.
- Remote `dev` and `main` CI verification remains pending at this audit version.

## Recommendation
Proceed to commit and push to `dev`, then verify `dev` CI. Promote to `main` only after `dev` is green, and verify `main` CI after promotion.
