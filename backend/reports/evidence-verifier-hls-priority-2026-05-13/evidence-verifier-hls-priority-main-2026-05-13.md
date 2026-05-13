# Evidence Verifier HLS Priority Main Promotion - 2026-05-13

## Scope

Promote the already validated dev slice for HLS fatal evidence priority onto current `main` without merging unrelated `dev` changes.

## Changed

- Applied the HLS fatal evidence priority changes from dev commit `4a6213e1e9e41f823885a4f2cb8316b6100b4839` to current main base `703c1551fa90072afcddf6c537ed869d597150ba`.
- `scripts/prod-runtime-evidence-bundle.sh` now preserves optional `hls-smoke-summary.json` in `production-runtime-evidence.tar.gz`.
- `scripts/prod-runtime-evidence-verify.sh` now reads optional `hls-smoke-summary.json` and prints `recommendedSlice=hls-fatal-fix` for fatal/failed/error HLS smoke evidence before using load-derived `nextFocus`.
- Tests cover HLS fatal priority for flat evidence, compressed tarball evidence, and bundle preservation.
- Added this main-promotion TODO and audit trail.

## Intentionally Not Changed

- Did not merge the full `dev` branch into `main` because `origin/main..origin/dev` contains many unrelated commits.
- Did not run direct production SSH.
- Did not claim the server has pulled or deployed the new main runtime image.
- Did not execute production preflight or Real Backend Test.
- Did not implement an HLS transcoding/upload fix itself.

## Objective Checklist

- Server pulls/deploys current `main` runtime image: not complete, server evidence still missing.
- Production preflight after deploy: not complete, server evidence still missing.
- Real Backend Test with `pageSize=12`, no seed, no cache, real Work/Study URLs: not complete, returned load evidence still missing.
- Result-based slice selection: strengthened for future evidence; not complete until actual returned evidence selects a slice.
- Selected slice implementation: not complete.
- Full E2E after selected slice: not complete.
- CI after selected slice: not complete.

## Validations

- `npm test -- --run src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts` passed with 26 tests on the main-based worktree.
- `bash -n scripts/prod-runtime-evidence-bundle.sh scripts/prod-runtime-evidence-verify.sh scripts/prod-real-load-steps.sh` passed.
- `npx eslint src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-real-load-steps.test.ts` passed.

## Risk

This prepares main for better evidence interpretation but does not itself satisfy the active runtime goal. The next proof still has to come from a server-side main image pull/deploy, preflight, and real load evidence bundle.

## Recommendation

Create a main-targeted PR for this minimal promotion, verify `CI Main Runtime` and `Publish GHCR Main`, then continue with the server evidence handoff instead of marking the active goal complete.
