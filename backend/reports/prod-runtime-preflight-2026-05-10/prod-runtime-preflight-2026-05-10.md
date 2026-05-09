# Production Runtime Preflight - 2026-05-10

## Scope
- Continue the active backend performance objective without repeating completed heavy-detail read optimizations.
- Add an operational preflight that proves Real Backend Test observability is live on the running production compose stack before interpreting load-test results.
- This slice does not change backend API behavior, DTO shape, EF queries, cache behavior, or load-test targets.

## Objective Mapping
- Stored thumbnail/public content/read-model work: already implemented in earlier slices; not repeated here.
- DB connection/pool pressure: this slice verifies the running backend exposes pool settings and DB command diagnostics samples through the existing admin diagnostics endpoint.
- JSON serialization/payload attribution: this slice verifies public JSON gzip is active and preserves existing target payload/receive metrics work.
- nginx/app timing completeness: this slice fails when public API responses miss `X-Nginx-Request-Time` or `X-App-Elapsed-Ms`.
- Production 2 CPU / 8 GiB alignment: this slice reports backend-visible `processor_count`, `memory_max`, and `cpu_max` from cgroups.

## Changes
- Added `scripts/prod-runtime-preflight.sh`.
- Added `src/test/prod-runtime-preflight.test.ts` with fake `docker` and `curl` binaries to exercise the script through its public CLI interface.
- Added `scripts/prod-runtime-preflight.sh` to `scripts/main-runtime-allowlist.txt` so runtime-only main promotion can carry it.
- Added `todolist-2026-05-10.md` for the current dated execution record.

## Preflight Checks
The script validates:
- `docker compose config` does not resolve `LoadTesting__BaseUrl` to backend-direct `http://127.0.0.1:8080`, `http://localhost:8080`, or `http://backend:8080`.
- `backend`, `frontend`, `nginx`, and `db` are running.
- backend env exposes `LoadTesting__BaseUrl` and `ASPNETCORE_ENVIRONMENT=Production` without printing raw env or connection strings.
- backend-visible resource data includes `processor_count`, `memory_max`, and `cpu_max`.
- public API through `BASE_URL` returns `X-Nginx-Request-Time` and `X-App-Elapsed-Ms`.
- public JSON responds with gzip when requested.
- when `REQUIRE_ADMIN_DIAGNOSTICS=1`, authenticated admin diagnostics expose DB command latency samples and Npgsql max pool.

## What Was Intentionally Not Changed
- No public API DTO or response schema was changed.
- No EF query/projection code was changed.
- No Npgsql pool size default was changed.
- No k6 scenario, RPS, max VUs, target selection, or page size was changed.
- No cache was introduced.
- No backend production code was changed in this slice.

## Validation
- RED: `npm test -- --run src/test/prod-runtime-preflight.test.ts` failed before implementation because `scripts/prod-runtime-preflight.sh` did not exist.
- PASS: `npm test -- --run src/test/prod-runtime-preflight.test.ts` passed 4/4.
- PASS: `npm test -- --run src/test/prod-runtime-preflight.test.ts src/test/load-test-dashboard.test.ts` passed 32/32.
- PASS: `npm run typecheck` passed.
- PASS: `for script in scripts/prod-runtime-preflight.sh scripts/promote-main-runtime.sh scripts/ci-compose-smoke.sh; do bash -n "$script"; done` passed.
- PASS: `git diff --check -- scripts/prod-runtime-preflight.sh src/test/prod-runtime-preflight.test.ts scripts/main-runtime-allowlist.txt todolist-2026-05-07.md` passed.
- PASS: `scripts/main-runtime-allowlist.txt` now contains `scripts/prod-runtime-preflight.sh`.

## Risks And Limits
- This script proves observability readiness; it is not itself a load test and does not improve latency directly.
- Real server execution was not performed in this local WSL environment because local Docker is unavailable here.
- Admin diagnostics verification requires an authenticated admin cookie file; without it the script skips diagnostics unless `REQUIRE_ADMIN_DIAGNOSTICS=1` is set.
- gzip verification depends on probing a public JSON endpoint through nginx/public origin.

## Recommended Next Step
Run this preflight on the production-like server after deployment and before the next Real Backend Test. If it passes, run the controlled constant-arrival-rate ladder on the real 2 CPU / total 8 GiB host. If it fails, fix the reported stale config/observability gap before tuning payload, DB pool, or CPU/GC behavior.
