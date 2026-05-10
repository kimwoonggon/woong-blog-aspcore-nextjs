# Production Redeploy Real Load Summary Output

Generated: 2026-05-10T22:44:00+09:00

## Summary

Updated the manual production redeploy GitHub Actions path so an optional production Real Backend Test run cannot finish without producing usable summary evidence. When `run_real_load=true`, the remote script now sets a deterministic `OUTPUT_DIR`, verifies the generated JSON and Markdown summaries exist, prints their paths, and emits the Markdown summary to the Actions log.

## Changed

- `.github/workflows/prod-runtime-redeploy.yml`
  - Passes `OUTPUT_DIR` into `scripts/prod-real-load-steps.sh`.
  - Verifies `prod-real-load-steps-summary.json` and `prod-real-load-steps-summary.md` exist and are non-empty.
  - Prints summary artifact paths.
  - Prints `Real load summary markdown:` followed by the generated markdown summary.
- `src/test/prod-runtime-redeploy-workflow.test.ts`
  - Adds workflow contract assertions for summary JSON/Markdown validation and log output.
- `todolist-2026-05-10.md`
  - Records RED/GREEN/verification progress for this slice.

## Intentionally Not Changed

- No production application code was changed.
- No Docker compose runtime settings were changed.
- No cache behavior was introduced.
- No load target semantics were changed: `pageSize=12`, real Work/Study targets, and seed/fixture rejection remain in place.
- No production redeploy was dispatched because required production SSH secrets are still unavailable.

## Goal Verification

| Requirement | Evidence | Status |
| --- | --- | --- |
| Real Backend Test output can guide next slice | Workflow now prints generated markdown summary after load steps | Improved |
| `pageSize=12`, no seed, no cache, real URLs | Existing workflow contract still asserts pageSize=12, seed rejection, and no cache string | Preserved |
| Production deploy/preflight/load actually executed | Still blocked by missing production SSH access | Blocked |
| CI validation | Local focused tests and typecheck passed; remote CI still pending until PR | Pending |

## Validation

- RED: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts` failed before implementation because the workflow did not contain summary artifact validation/output strings.
- PASS: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts` passed 1/1.
- PASS: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts src/test/prod-real-load-steps.test.ts src/test/publish-ghcr-tags.test.ts` passed 6/6.
- PASS: extracted remote heredoc `bash -n /tmp/prod-runtime-redeploy-remote-summary.sh` passed.
- PASS: `npm run typecheck` passed.
- PASS: `git diff --check -- .github/workflows/prod-runtime-redeploy.yml src/test/prod-runtime-redeploy-workflow.test.ts todolist-2026-05-10.md` passed.

## Risks And Follow-Up

- The active production objective remains blocked until real `PROD_SSH_*` secrets or direct SSH access are available.
- This change improves result capture after production access exists; it does not itself run production load.
- GitHub CI and main promotion still need to run for this workflow-only change.

## Recommendation

Push this branch to a PR targeting `dev`, watch CI, then allow the normal runtime promotion to `main`. After production SSH access is provided, rerun `prod-runtime-redeploy.yml` with `run_real_load=true`.
