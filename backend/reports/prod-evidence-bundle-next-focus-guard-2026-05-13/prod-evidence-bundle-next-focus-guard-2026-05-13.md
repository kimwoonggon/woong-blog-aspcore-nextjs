# Production Evidence Bundle Next Focus Guard Audit - 2026-05-13

## Summary
- Hardened `scripts/prod-runtime-evidence-bundle.sh` so invalid Real Backend Test `nextFocus` values fail before an evidence tarball is created.
- Added tests proving the bundle script rejects missing and unknown `nextFocus` values.
- Kept the final verifier guard intact; this change moves the same evidence quality gate earlier into artifact packaging.

## Changed
- `scripts/prod-runtime-evidence-bundle.sh`
  - Adds the same allowed `nextFocus` values used by the verifier: `increase-rate-or-extend-soak`, `db-pool-or-resource-pressure`, `payload-or-network-transfer`, `app-cpu-or-serialization`, `measure-more`.
  - Fails when the real-load summary omits `nextFocus`.
  - Fails when `nextFocus` is not one of the allowed producer values.
- `src/test/prod-runtime-evidence-bundle.test.ts`
  - Adds missing and unknown `nextFocus` regression tests.
- `todolist-2026-05-13.md`
  - Records backup, red/green result, and validation commands.

## Intentionally Not Changed
- No production SSH, server deploy, or remote command was run.
- No Real Backend Test was run from this environment.
- No load target weakening was introduced; pageSize=12, public-origin, no seed/fixture, and real read-target guards remain.
- No next implementation slice was selected because current-main server evidence is still missing.

## Goal Verification
- Active goal requires selecting a next slice from a valid current-runtime Real Backend Test result.
- This change does not complete the active goal; it prevents bad Real Backend Test summaries from being packaged as evidence before local verification.
- Server deploy, production preflight, returned Real Backend Test evidence, result-based slice selection, full E2E, and post-slice CI are still missing.

## Validations
- RED confirmed: `npm test -- --run src/test/prod-runtime-evidence-bundle.test.ts` failed because missing and unknown `nextFocus` were accepted before implementation.
- Passed: `npm test -- --run src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts` with 23 tests.
- Passed: `bash -n scripts/prod-runtime-evidence-bundle.sh scripts/prod-runtime-evidence-verify.sh scripts/prod-real-load-steps.sh`.
- Passed: `npx eslint src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts`.

## Risks And Follow-Up
- The allowed next-focus values are duplicated between bundle and verifier scripts. Keep them in sync with `scripts/prod-real-load-steps.sh`.
- The active goal remains blocked until a current-main server evidence bundle is returned and verified.

## Final Recommendation
- Keep this guard and promote it through `dev` and `main`; then use the current server handoff script to obtain a real evidence bundle.
