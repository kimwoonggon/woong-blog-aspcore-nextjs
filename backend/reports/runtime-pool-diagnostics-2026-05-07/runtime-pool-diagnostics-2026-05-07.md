# Runtime Pool Diagnostics Audit - 2026-05-07

## Summary
- Added runtime and DB pool metadata to Real Backend Test diagnostics so production screenshots can be interpreted against the actual backend container limits and configured Npgsql pool cap.
- Exposed process memory limit, CPU quota, DbContext pool size, Npgsql min/max pool size, and pool limit source in diagnostics snapshots.
- Surfaced the same metadata in the admin Real Backend Test dashboard parser, summary model, and UI.
- This is observability, not a cache or load-test target change. It does not tune pool sizes by itself; it makes stale deployment/config/resource mismatch visible before the next tuning slice.

## Changed
- `backend/src/WoongBlog.Infrastructure/Persistence/Diagnostics/PersistenceRuntimeDiagnostics.cs`
  - New persistence runtime diagnostics model that parses provider, DbContext pool size, and Npgsql min/max pool settings from configuration.
- `backend/src/WoongBlog.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`
  - Registers `PersistenceRuntimeDiagnostics` beside pooled DbContext setup.
- `backend/src/WoongBlog.Infrastructure/LoadTesting/LoadTestDiagnosticsSampler.cs`
  - Adds cgroup/process memory limit and CPU quota detection.
  - Adds DB pool metadata to available, unavailable, and error database diagnostics payloads.
- `backend/src/WoongBlog.Api/Common/Json/WoongBlogApiJsonSerializerContext.cs`
  - Includes the DB pool diagnostics contract in source-generated JSON metadata.
- `src/lib/load-test-dashboard.ts`
  - Parses and summarizes runtime limit and DB pool metadata.
- `src/components/admin/LoadTestDashboard.tsx`
  - Displays memory limit, visible CPU, CPU quota, DbContext pool, and Npgsql min/max pool.
- Tests updated across backend integration/component, frontend unit, and Playwright dashboard coverage.

## Intentionally Not Changed
- Did not change Npgsql pool cap or DbContext pool size in this slice.
- Did not change Real Backend Test scenario definitions, target selection, `pageSize`, seed selection, RPS, or max VUs behavior.
- Did not add caching.
- Did not alter public Work/Blog DTO semantics.
- Did not mark the full performance objective complete; this only completes the runtime/pool observability slice.

## Goal Verification
- Goal: expose CPU/memory limits and DB pool configuration in backend diagnostics.
  - Verified by backend focused tests and live diagnostics endpoint smoke.
- Goal: surface these fields in the Real Backend Test UI.
  - Verified by frontend unit tests and Playwright dashboard spec.
- Goal: avoid leaking secrets.
  - Verified by implementation shape: only provider, pool sizes, and source label are exposed; connection strings and credentials are not included.
- Goal: provide evidence to distinguish stale deployment/config/resource mismatch from app/DB pressure.
  - Verified by live dev smoke showing provider `Postgres`, DbContext pool `128`, Npgsql max `40`, and runtime limit fields present.

## Validations
- RED backend component diagnostics parser test failed before `PersistenceRuntimeDiagnostics` existed.
- RED backend diagnostics endpoint test failed before `process.memoryLimitBytes` and `process.cpuQuotaCores` existed.
- RED frontend dashboard summary test failed before runtime limit and pool summary fields existed.
- Focused frontend dashboard test: `npm test -- --run src/test/load-test-dashboard.test.ts`
  - Passed `28/28`.
- Focused backend component tests: `PersistenceRuntimeDiagnosticsComponentTests` plus run-scoped diagnostics compatibility.
  - Passed `3/3`.
- Focused backend integration tests for diagnostics endpoint and collector failure payload.
  - Passed `2/2`.
- Source-generated JSON resolver focused test for `LoadTestDatabasePoolDiagnostics`.
  - Passed `1/1`.
- Backend solution: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"`
  - Passed: Contract `1` skipped, Component `126`, Unit `59`, Architecture `35`, Integration `230`.
- Frontend typecheck: `npm run typecheck`
  - Passed.
- Frontend lint: `npm run lint`
  - Passed with `0` errors and `7` existing warnings.
- Playwright dashboard: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1`
  - Passed `2/2`.
- Dev compose health:
  - Backend `http://127.0.0.1:18080/api/health` returned HTTP 200.
- Live diagnostics endpoint smoke:
  - `process.memoryLimitBytes` present.
  - `process.cpuQuotaCores` present.
  - `database.pool.databaseProvider = Postgres`.
  - `database.pool.dbContextPoolSize = 128`.
  - `database.pool.npgsqlMinimumPoolSize = 0`.
  - `database.pool.npgsqlMaximumPoolSize = 40`.
  - `database.pool.npgsqlPoolLimitSource = connection-string`.
- `git diff --check`
  - Passed.

## Risks And Follow-Up
- The current dev container did not report cgroup CPU/memory quotas, so those fields were present but `null`. Production should show values if Docker/cgroup limits are configured.
- This slice reports pool config; it does not yet reduce pool pressure. Next tuning still needs real prod-like runs after deployment.
- DbContext pool size remains `128`; Npgsql max pool is controlled by connection string and currently `40` in dev compose.
- Current working tree also contains the HLS upload-limit fix because it interrupted this slice.
- Existing warning remains: `AWSSDK.Core` low severity advisory `GHSA-9cvc-h2w8-phrp`.

## Recommendation
- Ship this diagnostics slice to `dev` and verify CI before changing pool sizes. After deployment, run the same spike and compare reported CPU quota, memory limit, Npgsql max pool, DB connection open P95, and open connections.
