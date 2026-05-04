# Real Backend Scenario Model Audit - 2026-05-05

## Summary

Corrected the Real Backend Test model so k6 scenarios no longer look like the browser-generated user/concurrency load model.

Changed:
- Added `peakRate` and `startVUs` to the frontend real backend config and submitted payload.
- Added scenario-specific controls:
  - `public-api-rps`: Target RPS, duration, Max VUs cap.
  - `public-api-spike`: Base RPS, Peak RPS, duration, Max VUs cap.
  - `public-api-soak`: VUs and duration.
  - `public-api-stress`: Start VUs, Max VUs, and duration.
- Added an execution profile summary so the dashboard states whether the scenario is arrival-rate or VU based.
- Changed running all-zero k6 snapshots to display `summary pending` instead of result-looking `0` metrics.
- Extended backend start validation to accept and validate `PeakRate` and `StartVus`.
- Updated the k6 runner to pass `PEAK_RATE` and `START_VUS` into the generated script and use them for spike/stress options.

## Intentionally Not Changed

- Browser Test behavior and its `startUsers`, `maxUsers`, `stepUsers`, and `concurrency` model were not changed.
- Real backend live streaming during a k6 run was not added; k6 still publishes authoritative summary metrics at completion.
- Existing fake runner behavior was not expanded beyond compatibility with the updated run entry contract.
- Production deployment/promotion was not performed because a fresh full e2e run has not completed green after this correction.

## Goal Verification

- Browser Test and Real Backend Test settings are now visually and behaviorally separated.
- Spike now has an explicit Base RPS and Peak RPS, and backend validation rejects a peak below base.
- Stress now has an explicit Start VUs input, and backend validation keeps it within `1..MaxVus`.
- Soak hides RPS because it is a constant-VU model.
- Running k6 results no longer show `Requests 0`, `Latency 0 ms`, or app/client `0 ms` as if those were final metrics.

## Validation

- `npx vitest run src/test/load-test-dashboard.test.ts` passed 24/24.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests.RealLoadTestControlPlane" --no-restore` passed 4/4.
- `npm run typecheck` passed.
- `npm run lint` passed with six existing unrelated warnings.
- Docker frontend/backend images built successfully through `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build backend frontend nginx`; the initial default backend port `8080` start failed due Docker port forwarding, then the documented `18080` override started the stack.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated` passed 2/2.
- Live dev-stack k6 smoke for `public-api-spike`, `public-blogs-only`, `rate=1`, `peakRate=2`, `durationSeconds=10`, `maxVUs=5`, `startVUs=1` completed with `totalRequests=29`, `failedRequests=0`, `p95Ms=5.1`, and two Study target metric rows.
- `dotnet test backend/WoongBlog.sln --no-restore` passed all non-skipped backend suites: component 114, unit 56, architecture 35, integration 202. Contract provider verification remained skipped by suite policy.

## Risks And Follow-Up

- k6 still does not stream partial metrics during the run; the UI now labels that period as pending. Streaming can be added later if live in-run metrics are required.
- The first Docker start attempt failed on default backend port publishing. The working dev-stack command used `BACKEND_PUBLISH_PORT=18080`.
- Full e2e and branch promotion are still pending and should not be claimed complete until a fresh full e2e run passes.

## Recommendation

Keep this scenario-model correction, then run the fresh full e2e suite before pushing `dev` or promoting to `main`.
