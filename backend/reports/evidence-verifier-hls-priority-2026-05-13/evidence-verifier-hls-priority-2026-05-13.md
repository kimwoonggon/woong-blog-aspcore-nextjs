# Evidence Verifier HLS Priority - 2026-05-13

## Scope

This slice hardens production/current-main evidence handling so a returned HLS smoke failure can select `hls-fatal-fix` before load-derived DB/detail optimization slices.

## Changed

- `scripts/prod-runtime-evidence-verify.sh`
  - Reads optional `hls-smoke-summary.json` from an extracted evidence directory or returned tarball.
  - Prints `recommendedSlice=hls-fatal-fix` when the HLS smoke summary is fatal, failed, error, or contains `failed to process HLS`.
  - Keeps existing validations for current main SHA/digests, public HTTPS origin, `pageSize=12`, no seed/fixture targets, failed-rate limits, dropped-iteration limits, and known `nextFocus` values.
  - Maps non-HLS `nextFocus` values to explicit recommended slices.
- `scripts/prod-runtime-evidence-bundle.sh`
  - Copies optional `hls-smoke-summary.json` from `REAL_LOAD_DIR` into the returned `production-runtime-evidence.tar.gz`.
- `src/test/prod-runtime-evidence-verify.test.ts`
  - Covers HLS fatal priority for flat evidence directories.
  - Covers HLS fatal priority after extracting a returned compressed evidence tarball.
- `src/test/prod-runtime-evidence-bundle.test.ts`
  - Covers optional HLS smoke evidence preservation in the returned tarball.
- `todolist-2026-05-13.md`
  - Records RED/GREEN evidence and validation commands for this slice.

## Intentionally Not Changed

- Did not run or automate direct production SSH.
- Did not claim that the server has pulled/deployed current `main`.
- Did not claim production preflight or Real Backend Test evidence exists.
- Did not implement the actual HLS fatal upload fix.
- Did not alter real load target rules: lists remain `pageSize=12`; seed/fixture/cache workarounds remain disallowed.

## Goal Check

- Server pull/deploy current `main` runtime image: not satisfied in this slice.
- Production preflight after deploy: not satisfied in this slice.
- Real Backend Test with `pageSize=12`, no seed, no cache, real Work/Study URLs: not satisfied in this slice.
- Result-based next slice selection: partially strengthened. Returned evidence can now prioritize `hls-fatal-fix` when HLS smoke evidence is present and fatal/failed/error.
- Full E2E and CI after selected slice: not satisfied in this slice.

## Validations

- `npm test -- --run src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts` passed with 26 tests.
- `bash -n scripts/prod-runtime-evidence-bundle.sh scripts/prod-runtime-evidence-verify.sh scripts/prod-real-load-steps.sh` passed.
- `npx eslint src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts` passed.

## Risks And Follow-Up

- This does not produce HLS smoke evidence by itself; the server-side evidence step must place `hls-smoke-summary.json` in `REAL_LOAD_DIR` if HLS validation is run.
- Active goal remains blocked until a real returned evidence bundle proves current-main deploy, production preflight, and real backend load result.
- After valid evidence selects `hls-fatal-fix`, `public-detail-serialization-body-optimization`, or `db-index-optimization`, the selected code slice still needs implementation, full E2E, and CI.

## Recommendation

Keep the active goal open. Use this verifier/bundle hardening when the server evidence bundle is returned, then select the next implementation slice from verified `recommendedSlice` output.
