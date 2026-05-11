# Production Redeploy Evidence Verification - 2026-05-12

## Summary

The manual `Production Runtime Redeploy` workflow now verifies the production evidence bundle when `run_real_load=true`. It records the checked-out `main` SHA and the actually pulled backend/frontend image digests, passes them into `prod-runtime-evidence-bundle.sh`, and immediately verifies the resulting tarball with `prod-runtime-evidence-verify.sh`.

This does not complete the active production goal by itself. It strengthens the workflow path so that, once production SSH secrets exist and the workflow is dispatched, the workflow can produce verified evidence suitable for result-based slice selection.

## Changed

- `.github/workflows/prod-runtime-redeploy.yml`
  - Records `actual_sha="$(git rev-parse HEAD)"` after pulling `origin/main`.
  - Computes backend/frontend `RepoDigests` after `docker compose pull`.
  - Passes `MAIN_SHA`, `BACKEND_IMAGE_DIGEST`, and `FRONTEND_IMAGE_DIGEST` to `prod-runtime-evidence-bundle.sh`.
  - Runs `prod-runtime-evidence-verify.sh` against the produced `production-runtime-evidence.tar.gz`.
- `src/test/prod-runtime-redeploy-workflow.test.ts`
  - Requires the workflow to compute actual digests and run the verifier.

## Intentionally Not Changed

- No production SSH was attempted.
- No production secrets were read or written.
- No Real Backend Test was run from this workspace.
- No result-based performance/HLS slice was selected because fresh production evidence is still missing.

## Objective Coverage

| Objective Requirement | Evidence / Status |
| --- | --- |
| Server pulls/deploys latest main runtime images | Workflow path prepared; still requires production SSH secrets and execution |
| Production preflight after deploy | Workflow already runs preflight; unchanged |
| Real Backend Test with pageSize=12/no seed/no cache/real URLs | Workflow already delegates to `prod-real-load-steps.sh`; unchanged guard remains |
| Verified evidence for result-based next slice | Improved: workflow now runs `prod-runtime-evidence-verify.sh` with actual SHA/digests |
| Full E2E and CI after selected slice | Still blocked until production evidence exists and a slice is selected |

## Validation

- RED: `vitest --run src/test/prod-runtime-redeploy-workflow.test.ts --pool=threads --maxWorkers=2 --reporter=dot` failed because the workflow did not call `./scripts/prod-runtime-evidence-verify.sh`.
- GREEN focused: `PATH=/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/node_modules/.bin:$PATH vitest --pool=threads --maxWorkers=2 --reporter=dot --run src/test/prod-runtime-redeploy-workflow.test.ts` passed, 1 test.
- Related regression: `PATH=/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/node_modules/.bin:$PATH vitest --pool=threads --maxWorkers=2 --reporter=dot --run src/test/prod-runtime-redeploy-workflow.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-real-load-steps.test.ts src/test/prod-runtime-preflight.test.ts src/test/prod-public-origin-preflight.test.ts` passed, 6 files and 31 tests.
- Shell syntax: `bash -n scripts/prod-runtime-evidence-verify.sh scripts/prod-runtime-evidence-bundle.sh scripts/prod-real-load-steps.sh scripts/prod-runtime-preflight.sh scripts/prod-public-origin-preflight.sh` passed.
- Audit JSON parse: `node -e "JSON.parse(...)"` passed.
- Focused lint: `eslint src/test/prod-runtime-redeploy-workflow.test.ts` passed.
- Diff hygiene: `git diff --check` passed.

## Recommendation

After this change reaches `main`, configure the missing production SSH secrets or run the server-side command manually. Dispatch the workflow with `run_real_load=true` only after the server path and `.env.prod` are ready.
