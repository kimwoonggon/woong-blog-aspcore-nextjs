# Production Runtime Redeploy Workflow - Audit

Generated at: 2026-05-10T20:40:00+09:00

## Summary

Added a manual GitHub Actions workflow that can, once production SSH secrets are configured, SSH to the production host, pull/deploy the latest `main` runtime images, run `prod-runtime-preflight.sh`, and optionally run `prod-real-load-steps.sh`.

## Changed

- Added `.github/workflows/prod-runtime-redeploy.yml`.
- Added `src/test/prod-runtime-redeploy-workflow.test.ts`.
- Added `.github/workflows/prod-runtime-redeploy.yml` to `scripts/main-runtime-allowlist.txt`.
- Updated `todolist-2026-05-10.md` with this slice and current validation evidence.

## Intentional Non-Changes

- Did not run production deploy because no working SSH credential/deploy secret exists.
- Did not add automatic `push` or `pull_request` trigger; the workflow is `workflow_dispatch` only.
- Did not disable host-key checking; the workflow requires `PROD_SSH_KNOWN_HOSTS` and uses strict host-key checking.
- Did not change cache behavior, load-test target page size, or production application code.

## Goal Mapping

- Production pull/deploy/preflight: prepared a manual CI path, but still requires secrets and a successful run.
- Real Backend Test rerun: prepared optional post-preflight path with real Work/Study targets and `pageSize=12` Study discovery, but not executed.
- Next performance/HLS slice: still blocked until a valid post-deploy production result exists.

## Validation

- RED: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts` failed before the workflow existed.
- PASS: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts` passed 1/1.
- PASS: `npm test -- --run src/test/prod-runtime-redeploy-workflow.test.ts src/test/publish-ghcr-tags.test.ts src/test/compose-loadtesting-baseurl.test.ts` passed 8/8.
- PASS: extracted workflow remote heredoc and `bash -n /tmp/prod-runtime-redeploy-remote.sh` passed.
- PASS: `npm run typecheck` passed.
- PASS: `npm run lint` passed with existing warnings only: 0 errors, 7 warnings.
- PASS: `git diff --check -- .github/workflows/prod-runtime-redeploy.yml src/test/prod-runtime-redeploy-workflow.test.ts scripts/main-runtime-allowlist.txt todolist-2026-05-10.md` passed.
- BLOCKED: `npx --yes actionlint .github/workflows/prod-runtime-redeploy.yml` failed with `npm error could not determine executable to run`; actionlint was not available through this npx package name.

## Risks / Follow-Up

- The workflow cannot run until these repository secrets exist: `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_PRIVATE_KEY`, and `PROD_SSH_KNOWN_HOSTS`.
- `PROD_SSH_PORT` and `PROD_GHCR_TOKEN` are optional.
- This does not complete the active goal; it only creates a reusable deploy/preflight/load execution path for when credentials are available.

## Recommendation

Configure the production SSH secrets and run `Production Runtime Redeploy` manually with `run_real_load=false` first. If preflight passes, rerun with `run_real_load=true` or run the existing pasteable server block 2.
