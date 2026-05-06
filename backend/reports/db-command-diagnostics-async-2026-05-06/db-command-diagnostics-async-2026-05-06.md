# Async DB Command Diagnostics Audit - 2026-05-06

## Goal
Fix the load-test observability gap where EF Core async query paths did not populate database command latency metrics. This slice supports later backend performance work by making DB command p95/p99 attribution available for async public read paths.

## Changed
- Added async `DbCommandInterceptor` overrides for reader, scalar, non-query, and command failure callbacks in `LoadTestDbCommandDiagnosticsInterceptor`.
- Added a Postgres integration test that executes async EF read and non-query commands through the diagnostics interceptor and verifies command latency samples are recorded.
- Fixed standalone k6 load scripts so `maxVUs` is correctly assigned from the parsed `MAX_VUS` environment value.
- Captured a scoped k6 smoke result under `loadtest/k6-public-works-100rps-15s.json`.

## Intentionally Not Changed
- No cache was added.
- Real Backend Test target semantics were not changed: no seed-only target priority and no `pageSize=12` reduction.
- Public DTO shape and public/admin content model were not changed in this slice; those were handled in the previous public detail body split slice.
- No Npgsql pool-size or query-count reduction was made here. This slice makes DB timing visible so those reductions can be measured accurately in later slices.
- The authenticated runtime diagnostics endpoint was not bypassed for local smoke testing.

## Verification Against Goals
- Async EF command timing is now covered by production interceptor code and a Postgres-backed integration test.
- The test exercises both async read (`AnyAsync`) and async non-query (`ExecuteSqlRawAsync`) paths, which cover the dominant EF command paths used by public read and operational flows.
- The standalone k6 scripts can now run instead of failing before traffic generation due to an undefined `maxVUs` symbol.

## Validations Performed
- RED: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~CommandDiagnosticsInterceptor_RecordsAsyncEfCommands_WithPostgres" --no-restore` failed before implementation because async EF commands did not increment `CommandLatency.SampleCount`.
- GREEN: the same focused Postgres integration test passed after adding async interceptor overrides.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests|FullyQualifiedName~StartupCompositionTests" --no-restore` passed 17/17.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed Component 121/121, Unit 56/56, Architecture 35/35, Integration 205/205, Contract skipped due no pact files.
- PASS: `npm run lint` completed with 0 errors and the same 6 pre-existing warnings.
- PASS: `git diff --check` completed with no whitespace errors.
- PASS: local runtime smoke after backend rebuild returned health `200` and public works list `200`.
- BLOCKED: local `/api/admin/load-test/diagnostics` returned `401` and test-login returned `404`, so runtime command-latency endpoint readback was not verified locally.
- RED: standalone k6 smoke initially failed with `ReferenceError: maxVUs is not defined`.
- GREEN: after the script fix, standalone k6 smoke completed `1,501` requests at `100.05 rps`, failure rate `0`, p95 `4.16 ms` against `/api/public/works?page=1&pageSize=12`.
- PASS: `k6 inspect` for `scripts/loadtest/public-api-spike.js` succeeded and resolved `maxVUs`.

## Risks And Yellow Flags
- The runtime UI still needs a real authenticated Real Backend Test run after deployment to confirm `db command p95` is populated end-to-end.
- This change records async EF timings but does not itself reduce DB roundtrips, pool pressure, serialization allocation, or heavy detail latency.
- The local k6 smoke is a narrow sanity check, not a 1000 rps capacity proof.
- Existing `AWSSDK.Core` NU1901 warnings remain unrelated to this slice.

## Recommendation
Merge this instrumentation slice first so the next heavy-detail optimization can be evaluated with DB command latency visible. The next implementation target should be stored representative image metadata and detail-query roundtrip reduction, not cache.
