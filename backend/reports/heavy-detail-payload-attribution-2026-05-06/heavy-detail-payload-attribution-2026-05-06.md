# Heavy Detail Payload Attribution Audit - 2026-05-06

## Goal
Identify where further backend performance improvement is possible by adding Real Backend Test attribution for heavy public detail payload size and receive timing, without weakening the workload.

## Changed
- Added target-level k6 trends for response body bytes and `response.timings.receiving`.
- Extended `RealLoadTestTargetMetrics` with `ResponseBytesP95` and `ReceiveP95Ms`.
- Updated the fake real runner so development/control-plane responses include plausible payload and receive timing metrics.
- Updated frontend Real Backend parsing and target summary UI to display `Payload P95` and `Receive P95` per target.
- Added backend and frontend tests proving the new fields are recorded and preserved.

## Intentionally Not Changed
- No cache was added.
- Real Backend target selection was not changed.
- Public Work/Blog list targets still use `pageSize=12`.
- Seeded content was not prioritized.
- Public DTO response shape and public endpoint query behavior were not changed.
- No DB schema, query, or index change was made in this slice.

## Goal Verification
This slice does not directly reduce p95 latency. It improves the load-test evidence so future runs can distinguish these cases:

- High client/app p95 with high DB command/open p95: DB/query/pool pressure is likely.
- High client/app p95 with low DB p95 but large `ResponseBytesP95`: large DTO/payload or serialization/transfer is likely.
- High client p95 with high `ReceiveP95Ms`: response transfer/client receive path is likely contributing.
- High app elapsed with low DB and low receive time: app CPU/serialization/object materialization should be inspected.

## Validation
- RED: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~RealLoadTestRunnerComponentTests" --no-restore --logger "console;verbosity=minimal"` failed before implementation because k6 did not record `response_bytes`.
- RED: `npm test -- --run src/test/load-test-dashboard.test.ts` failed before implementation because `responseBytesP95` and `receiveP95Ms` were dropped.
- PASS: focused backend component test passed 2/2.
- PASS: focused frontend dashboard parser test passed 28/28.
- PASS: focused Real Backend control-plane integration test passed 1/1.
- PASS: `npm run typecheck`.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed: Contract 1 skipped, Component 123, Unit 56, Architecture 35, Integration 224. Existing AWSSDK.Core NU1901 warnings remain.
- PASS: `npm run lint` completed with 0 errors and 6 existing warnings.
- PASS: `git diff --check`.
- PASS: diff guard found no `pageSize=1`, seed-priority, or cache shortcut changes outside TODO/report artifacts.
- PASS: `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` rebuilt and restarted local compose.
- PASS: nginx and backend direct health checks returned 200.
- PASS: backend runtime confirmed `LoadTesting__BaseUrl=http://nginx`.
- PASS: live Real Backend k6 smoke `20260506-123052-public-api-rps-7e5cdcea`, 5rps/5s/maxVUs 20, current public list-selected targets, list `pageSize=12`: 26 requests, 0 failed, p95 19.2ms, p99 56.1ms.
- PASS: every smoke target reported `responseBytesP95` and `receiveP95Ms`.
- PASS: Playwright real-backend dashboard mock flow passed 1/1.

## Live Smoke Target Metrics
| Target | P95 | Payload P95 | Receive P95 |
| --- | ---: | ---: | ---: |
| works-list | 5.3 ms | 4,422 B | 0.2 ms |
| work-read | 18.7 ms | 516 B | 0.1 ms |
| study-list | 6.7 ms | 3,869 B | 0.1 ms |
| study-read | 48.2 ms | 423 B | 0.1 ms |

Artifacts:
- `backend/reports/heavy-detail-payload-attribution-2026-05-06/loadtest/runtime-smoke-summary.json`
- `backend/reports/heavy-detail-payload-attribution-2026-05-06/loadtest/runtime-metrics.json`
- `backend/reports/heavy-detail-payload-attribution-2026-05-06/loadtest/runtime-start-request.json`

## Risks And Yellow Flags
- `responseBytesP95` prefers `Content-Length` when available and otherwise falls back to `response.body.length`; for compressed/chunked responses this should be treated as attribution, not exact wire-byte accounting.
- `ReceiveP95Ms` is k6 client receive timing. It does not equal server-side JSON serialization time by itself.
- The smoke run used current first public list items and was intentionally small; it proves metric plumbing, not production capacity.
- Existing AWSSDK.Core NU1901 low-severity warnings remain unrelated to this change.

## Recommendation
Promote this measurement slice first. Then run realistic 100/300/500 rps public mix tests and choose the next structural performance change from evidence:

1. If detail `ResponseBytesP95` is high while DB p95 stays low, split heavy public detail payload or defer large content fields by route/client need.
2. If DB command/open p95 rises, continue query/index/pool work.
3. If app elapsed rises with low DB and low receive timing, inspect JSON serialization/object materialization allocations next.
