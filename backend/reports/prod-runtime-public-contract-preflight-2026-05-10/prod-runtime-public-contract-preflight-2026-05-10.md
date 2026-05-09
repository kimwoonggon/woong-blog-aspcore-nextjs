# Production Public Contract Preflight Guard - 2026-05-10

## Scope
- Continue the backend performance objective without repeating completed heavy-detail read optimizations.
- Add a production preflight guard that detects stale public Work video DTO responses before interpreting Real Backend Test results.
- This slice does not change backend API implementation, EF queries, DTO definitions, nginx config, Docker config, cache behavior, or load-test targets.

## Reason
A current public production probe returned Work video fields that are no longer exposed by the repo's current `PublicWorkVideoDto`:

- `originalFileName`
- `fileSize`
- `createdAt`

That means production load-test numbers can still include old public video/admin payload behavior and old request-time costs. Running the new load ladder before detecting this would risk optimizing stale behavior that has already been fixed in code.

## Changed
- `scripts/prod-runtime-preflight.sh`
  - Added `REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1`.
  - Requires `WORK_READ_PATH` when the contract check is enabled.
  - Probes a real public Work detail path supplied by the operator.
  - Requires the target to include at least one video so the contract is meaningful.
  - Fails if public video payload still contains stale admin/mutation-only fields: `originalFileName`, `fileSize`, or `createdAt`.
- `src/test/prod-runtime-preflight.test.ts`
  - Added fake runtime coverage for current public video DTO shape.
  - Added stale-runtime failure coverage.
  - Added missing `WORK_READ_PATH` failure coverage.
- `todolist-2026-05-10.md`
  - Recorded this slice, user-instruction mapping, RED/GREEN status, and validation results.

## Intentionally Not Changed
- No backend query/projection code was changed.
- No public DTO C# definitions were changed.
- No frontend public parser was changed.
- No DB pool settings were changed.
- No nginx timing/header config was changed.
- No cache was introduced.
- No production load test was executed against stale runtime.

## Goal Verification
- Goal: avoid stale deployment/config invalidating Real Backend Test interpretation.
  - Satisfied. The preflight can now fail when a real public Work detail response still exposes removed public video fields.
- Goal: keep real targets and avoid seed shortcuts.
  - Satisfied. The guard requires an operator-provided `WORK_READ_PATH`; it does not choose a seeded/default read target.
- Goal: improve bottleneck attribution before the next optimization.
  - Satisfied. The new guard prevents old public video payload and request-time behavior from being mistaken for current app CPU/serialization/network cost.
- Goal: do not repeat completed heavy-detail query work.
  - Satisfied. This slice only strengthens preflight verification.

## Validation
- RED: `npm test -- --run src/test/prod-runtime-preflight.test.ts`
  - Failed before implementation because the script did not require `WORK_READ_PATH` or detect stale public video fields.
- GREEN focused: `npm test -- --run src/test/prod-runtime-preflight.test.ts`
  - Passed 6/6.
- GREEN related Vitest bundle: `npm test -- --run src/test/prod-runtime-preflight.test.ts src/test/prod-real-load-steps.test.ts src/test/load-test-dashboard.test.ts`
  - Passed 37/37.
- Shell syntax: `for script in scripts/prod-runtime-preflight.sh scripts/prod-real-load-steps.sh scripts/promote-main-runtime.sh scripts/ci-compose-smoke.sh; do bash -n "$script"; done`
  - Passed.
- Whitespace: `git diff --check -- scripts/prod-runtime-preflight.sh src/test/prod-runtime-preflight.test.ts todolist-2026-05-10.md`
  - Passed.
- TypeScript: `npm run typecheck`
  - Passed.

## Operator Command
Run this on the production server before the load ladder, using a current real public Work detail that has at least one video:

```bash
BASE_URL=https://woonglab.com \
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 \
WORK_READ_PATH=/api/public/works/<real-video-work-slug> \
./scripts/prod-runtime-preflight.sh
```

With admin diagnostics required:

```bash
BASE_URL=https://woonglab.com \
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 \
WORK_READ_PATH=/api/public/works/<real-video-work-slug> \
REQUIRE_ADMIN_DIAGNOSTICS=1 \
ADMIN_COOKIE_FILE=/tmp/admin.cookies \
./scripts/prod-runtime-preflight.sh
```

## Risks And Follow-Up
- The contract check intentionally requires a Work detail response with at least one video. A no-video target cannot prove the public video DTO shape.
- The check uses exact JSON field-name detection and is designed for operational preflight, not schema validation of every public response.
- Current public production probes still looked stale during this slice. The server must pull/recreate the newly published runtime image before load-test interpretation is valid.
- After this preflight passes on server, run `scripts/prod-real-load-steps.sh` with real Work/Study read targets and use its aggregate `nextFocus` result for the next code slice.

## Recommendation
Deploy/recreate the production runtime, run `prod-runtime-preflight.sh` with `REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1`, then run the real load step ladder. If the preflight fails, fix deployment/runtime freshness before making more backend performance code changes.
