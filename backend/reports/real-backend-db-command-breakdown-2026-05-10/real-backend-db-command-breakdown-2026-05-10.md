# Real Backend DB Command Breakdown Audit - 2026-05-10

## Summary
- Added DB command p95 to the Real Backend Test latency breakdown model used by the dashboard.
- `summarizeRealBackendRunSnapshot` now preserves DB command p95 from the run-wide diagnostics aggregate when the final diagnostics sample has no command samples.
- The Client/nginx/app/db panel now displays the aggregate DB command p95 and its source instead of reading only the latest diagnostics sample.

## Intentionally Not Changed
- No backend query, DTO payload, cache, load-test target, `pageSize`, k6 scenario, or Docker runtime setting was changed.
- No DB connection pool sizing or EF Core query shape was changed in this slice.
- No production deployment command was run from this workspace.

## Goal Verification
- Goal: make backend load-test bottleneck interpretation clearer by preventing DB command p95 from disappearing in the component timing breakdown.
- Result: satisfied for this slice. A valid DB command p95 captured earlier in the run remains visible even if the final diagnostics sample is idle.
- Non-goal: improve raw backend throughput or p95 latency. This slice improves observability, not runtime performance.

## Validations
- RED confirmed: `npm test -- --run src/test/load-test-dashboard.test.ts -t "keeps DB command p95"` initially failed because `dbCommandP95Ms` and `dbCommandP95Source` were absent.
- GREEN confirmed: `npm test -- --run src/test/load-test-dashboard.test.ts -t "keeps DB command p95"` passed 1/1.
- Regression suite: `npm test -- --run src/test/load-test-dashboard.test.ts` passed 29/29.
- TypeScript: `npm run typecheck` passed.
- Whitespace: `git diff --check -- src/lib/load-test-dashboard.ts src/components/admin/LoadTestDashboard.tsx src/test/load-test-dashboard.test.ts todolist-2026-05-10.md` passed.

## Risks And Follow-Up
- This does not add per-target DB command attribution; DB command p95 remains run-level diagnostics.
- If diagnostics collection itself is disabled or unavailable, DB command p95 still correctly remains unavailable.
- Next performance slice should target either per-target DB attribution or a production load rerun after this observability fix is deployed.

## Recommendation
- Merge this observability fix through dev/main, then rerun a real backend test and compare `client p95`, `ASP.NET app elapsed p95`, and `db command p95` together before choosing the next structural performance change.
