# Backend Load-Test Observability Audit - 2026-05-04

## Summary

Implemented the `2026-05-04-backend-loadtest.md` goal within the existing Next.js frontend, ASP.NET Core backend, nginx, and Docker Compose structure.

The existing `/admin/load-test` HTTP read-load flow remains in place. The dashboard now supports typed `step`, `soak`, and `spike` load patterns and samples backend diagnostics during a run. The new diagnostics path is admin-only and reports process memory, GC heap/collection/time data, ThreadPool queue/thread data, and DB probe state.

## Changed

- Added typed load pattern, threshold, diagnostics payload, and runtime summary helpers in `src/lib/load-test-dashboard.ts`.
- Extended `src/components/admin/LoadTestDashboard.tsx` with:
  - load pattern selector
  - soak/spike planning fields
  - backend runtime panel
  - current, peak, and delta metric display
  - Green/Yellow/Red/Unavailable health states
  - runtime diagnostics polling while a run is active
- Added admin-only ASP.NET Core endpoint:
  - `GET /api/admin/load-test/diagnostics`
  - implemented in `backend/src/WoongBlog.Api/Modules/Diagnostics/LoadTestDiagnosticsEndpoint.cs`
- Registered the diagnostics endpoint from `Program.cs`.
- Expanded frontend unit, Playwright, and backend integration coverage.

## Not Changed

- No SignalR was introduced.
- No k8s, HPA, pod restart, or Redis backplane observability was introduced.
- No NBomber or C# Worker simulator was introduced.
- No new npm or NuGet dependency was added.
- Public Work/Study API routes and existing load-test target request behavior were preserved.
- The dashboard remains an HTTP load-test tool rather than a real persistent-connection simulator.

## Requirement Check

- Existing HTTP load-test behavior preserved: yes.
- `step`, `soak`, `spike` modeled in typed config: yes.
- Runtime/DB metrics visible on `/admin/load-test`: yes.
- Backend diagnostics endpoint added: yes.
- Metric collection failure handled as unavailable/error in UI: yes.
- Threshold health states implemented: yes.
- Tests added or updated: yes.
- Mandatory audit artifact generated: yes.

## Validations

- `npx vitest run src/test/load-test-dashboard.test.ts`
  - Passed: 10/10.
- `npm run typecheck`
  - Passed.
- `npx eslint src/lib/load-test-dashboard.ts src/components/admin/LoadTestDashboard.tsx src/test/load-test-dashboard.test.ts tests/admin-load-test-dashboard.spec.ts`
  - Passed.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~StartupCompositionTests"`
  - Passed: 8/8.
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build frontend nginx`
  - Passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test tests/admin-load-test-dashboard.spec.ts --workers=1`
  - Passed: 1/1.
- `npm run lint`
  - Completed with 0 errors and 6 pre-existing warnings in unrelated files.
- `dotnet test backend/WoongBlog.sln`
  - Passed: 402 passed, 1 skipped.
  - Existing NU1901 warnings remain for `AWSSDK.Core`.

## Yellow Flags

- The DB diagnostics endpoint reports PostgreSQL connection counts only when the active relational provider uses Npgsql. Test/InMemory mode correctly reports DB diagnostics as unavailable.
- DB timeout count is process-local and increments only when the diagnostics probe sees timeout-like failures. It is not a full Npgsql pool telemetry source.
- Soak/spike are modeled and surfaced in the current HTTP-runner shape. This does not create true long-lived users or persistent connections.
- The first Playwright attempt used `PLAYWRIGHT_BASE_URL=http://127.0.0.1` and failed because global setup tried port 80. The corrected compose URL `http://127.0.0.1:3000` passed.

## Recommendation

Accept this as the current-scope implementation. The next useful increment would be adding an exportable run summary so HTTP p95 trends and runtime/DB pressure snapshots can be kept after each load-test run.
