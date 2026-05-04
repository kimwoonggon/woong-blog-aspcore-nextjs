# Real Backend Load Test Gap Audit - 2026-05-05

## Scope

Fixed the Real Backend Test path reported from `2026-05-04-backend-loadtest-additional.md`, especially the failing `public-api-spike` / `public-blogs-only` / `k6` / `10` / `30` / `10` start flow that returned 400 and left run IDs and metrics empty.

## Changed

- Added real backend target payload support so the dashboard posts editable `Work list`, `Work read`, `Study list`, and `Study read` URLs to the backend runner path.
- Added backend contracts for real-run targets, per-target metrics, and latency breakdown fields.
- Added k6 runner dispatch and `K6RealLoadTestRunner` so `k6` no longer depends on a missing implementation.
- Added fake-runner target cycling and target metrics to keep test mode behavior aligned with real runner behavior.
- Narrowed runner choices to supported `k6` and `fake` values.
- Added Docker runtime k6 installation via `grafana/k6:1.0.0`.
- Set dev compose backend `LoadTesting__BaseUrl` to `http://nginx` so containerized k6 hits the same proxy path as the browser-facing stack.
- Added nginx response timing headers for API routes and updated the dashboard to render per-target real backend summaries plus available component timing.
- Routed real backend start/stop UI mutations through the shared CSRF helper so browser clicks no longer fail with request validation 400.

## Intentionally Not Changed

- Database timing is not fabricated. DB remains unavailable unless backend diagnostics later expose per-request DB timing.
- `nginxUpstreamP95Ms` remains nullable because nginx may not emit a parseable upstream value for every route or response path.
- Existing unrelated dirty files and prior reports were not reverted.
- The full Playwright suite was not rerun; validation was focused on the load-test dashboard path plus build/type/lint/backend checks.

## Goal Verification

- 400 start failure: fixed. The exact failing shape started with HTTP 200 in a live compose smoke.
- UI browser start failure: fixed. The dashboard start/stop mutations now attach CSRF headers, and the same shape starts with HTTP 200 from an actual UI click.
- Run ID missing: fixed. Live run returned `20260504-171457-public-api-spike`.
- Requests and metrics stuck at zero: fixed. Live run completed with 402 requests and 0 failures.
- Work/Study URL model: fixed. Frontend tests verify `public-blogs-only` posts Study list/read targets, and Playwright verifies the posted real backend payload.
- Similar metrics: fixed. Real backend status/metrics now include request counts, average/current RPS, HTTP status counts, p95/p99/max latency, latency breakdown, and per-target summaries.
- Latency breakdown: partially source-dependent by design. Live API-target run captured app elapsed p95 and nginx request-time p95; DB and nginx upstream timing remain unavailable when the sources do not provide them.

## Validation

- `npx vitest run src/test/load-test-dashboard.test.ts`: passed 22/22.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests.RealLoadTestControlPlane" --no-restore`: passed 3/3.
- `dotnet build backend/WoongBlog.sln --no-restore`: succeeded with existing AWSSDK.Core NU1901 warnings.
- `npm run typecheck`: passed.
- `npm run lint`: passed with existing warnings outside this task scope.
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build backend frontend nginx`: built and started the dev stack.
- `docker compose -f docker-compose.dev.yml exec -T backend k6 version`: returned `k6 v1.0.0`.
- `curl -fsS http://127.0.0.1:3000/api/health`: returned healthy API JSON.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core npx playwright test tests/admin-load-test-dashboard.spec.ts --workers=1`: passed 2/2.
- Live exact-shape smoke: `public-api-spike` / `public-blogs-only` / `k6` / `10` / `30` / `10` started with HTTP 200 and completed with `totalRequests=402`, `failedRequests=0`, `p95Ms=5.5`, `p99Ms=5.5`, `appElapsedP95Ms=5.2`, `nginxRequestTimeP95Ms=6`, and two Study target metrics with 201 requests each.
- Follow-up actual UI click smoke after CSRF fix: `public-api-spike` / `public-blogs-only` / `k6` / `10` / `30` / `10` started with HTTP 200 and displayed Study list/read target metrics instead of `Run ID not started`.
- `npm run typecheck`: passed after the CSRF UI fix.
- `npm run lint`: passed after the CSRF UI fix with the same existing unrelated warnings.

## Risks And Follow-Up

- `LoadTesting__BaseUrl` must be set correctly outside dev compose so k6 targets the intended proxy/app endpoint.
- Docker builds now need access to the pinned k6 image when the local Docker cache is cold.
- Existing AWSSDK.Core NU1901 warnings remain and should be handled separately.
- Real-time status still shows zero until k6 completes because the current runner ingests the summary at completion; streaming k6 output would be a separate improvement.

## Recommendation

Use the current implementation for the Real Backend Test path. Follow up only if DB-level timing or live in-run k6 progress streaming becomes a product requirement.
