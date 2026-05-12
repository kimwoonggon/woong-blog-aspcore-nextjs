# Production Evidence Next Focus Guard Audit - 2026-05-13

## Summary
- Hardened `scripts/prod-runtime-evidence-verify.sh` so a returned Real Backend Test summary must include an actionable `nextFocus` value.
- Restricted accepted `nextFocus` values to the values produced by `scripts/prod-real-load-steps.sh`.
- Added regression tests proving summaries with missing or unknown `nextFocus` are rejected.

## Changed
- `scripts/prod-runtime-evidence-verify.sh`
  - Adds an allow-list for `nextFocus`: `increase-rate-or-extend-soak`, `db-pool-or-resource-pressure`, `payload-or-network-transfer`, `app-cpu-or-serialization`, `measure-more`.
  - Fails evidence verification when `prod-real-load-steps-summary.json` lacks `nextFocus`.
  - Fails evidence verification when `nextFocus` is not one of the producer-supported values.
- `src/test/prod-runtime-evidence-verify.test.ts`
  - Updates the valid fixture from a non-producer value to `app-cpu-or-serialization`.
  - Adds tests for missing and unknown `nextFocus`.
- `todolist-2026-05-13.md`
  - Records backup, red/green result, and validation commands for this slice.

## Intentionally Not Changed
- No production SSH, deployment, or server command was executed.
- No Real Backend Test was run from this environment.
- No load target behavior was weakened: `pageSize=12`, public HTTPS origin, real public read path, and seed/fixture guards remain intact.
- No next code slice was selected because current-main server deploy/preflight/load evidence is still missing.

## Goal Verification
- Active goal requires selecting a next slice from a valid current-runtime Real Backend Test result.
- This change does not complete that goal; it strengthens the local evidence gate so a returned load result must carry a known `nextFocus` before it can be used for result-based slice selection.
- The current active-goal blockers remain server-side deploy evidence, production preflight evidence, and valid Real Backend Test evidence.

## Validations
- RED confirmed: `npm test -- --run src/test/prod-runtime-evidence-verify.test.ts` failed because missing and unknown `nextFocus` were accepted before implementation.
- Passed: `npm test -- --run src/test/prod-runtime-evidence-verify.test.ts` with 11 tests.
- Passed on clean `origin/dev` worktree: `npm test -- --run src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts src/test/prod-runtime-evidence-bundle.test.ts` with 21 tests.
- Passed: `bash -n scripts/prod-runtime-evidence-verify.sh`.
- Passed: `npx eslint src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts src/test/prod-runtime-evidence-bundle.test.ts`.

## Risks And Follow-Up
- The verifier still depends on receiving a server-generated evidence bundle. Without that bundle, the active goal cannot proceed to slice selection.
- `nextFocus` is a focus signal, not a full implementation plan. After valid evidence is returned, map it explicitly to HLS fatal fix, public detail serialization/body optimization, or DB/index optimization before coding the selected slice.

## Final Recommendation
- Keep this guard. It prevents accepting Real Backend Test evidence that cannot support the required result-based next-slice decision.
