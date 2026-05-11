# Prod Runtime Evidence Bundle Audit - 2026-05-11

## Goal

Make the remaining production objective auditable after the server operator runs it. The repository now provides a standard evidence bundle that ties together production preflight output, Real Backend Test output, runtime image metadata, and the result-based next-slice focus.

## Changed

- Added `scripts/prod-runtime-evidence-bundle.sh`.
- Added `src/test/prod-runtime-evidence-bundle.test.ts`.
- Updated `.github/workflows/prod-runtime-redeploy.yml` to tee production preflight output to a log and create an evidence bundle after `prod-real-load-steps.sh` completes.
- Added `scripts/prod-runtime-evidence-bundle.sh` to `scripts/main-runtime-allowlist.txt` so it can be promoted to `main`.
- Updated `src/test/prod-runtime-redeploy-workflow.test.ts` to verify the workflow wiring and allowlist entry.
- Updated `todolist-2026-05-11.md`.

## What The Bundle Verifies

- Preflight log exists.
- Preflight log contains `PASS`.
- Preflight log contains nginx request timing evidence.
- Preflight log contains app timing evidence.
- Preflight log contains gzip evidence.
- Preflight log contains public Work detail contract freshness evidence.
- Real load summary JSON and Markdown exist.
- Real load summary has at least one step.
- Real load summary uses `listPageSize=12`.
- Real load target paths do not contain `seed` or `fixture`.
- Real load base URL does not bypass the public nginx path via backend-direct localhost/backend URLs.

## Generated Evidence

When run on the production server after preflight and real load, the script writes:

- `production-runtime-evidence-manifest.json`
- `production-runtime-evidence-summary.md`
- `production-runtime-evidence.tar.gz`

## Intentionally Not Changed

- No production SSH was used from this workspace.
- No production command was executed from this workspace.
- No production secret was read or modified.
- No performance slice was selected without a real production load summary.

## TDD Evidence

- RED:
  - `npx vitest run src/test/prod-runtime-evidence-bundle.test.ts --pool=threads --maxWorkers=2`
  - Failed because `scripts/prod-runtime-evidence-bundle.sh` did not exist.
- GREEN:
  - `npx vitest run src/test/prod-runtime-evidence-bundle.test.ts --pool=threads --maxWorkers=2`
  - Passed, 3 tests.

## Validation

- `bash -n scripts/prod-runtime-evidence-bundle.sh`: passed.
- `npx vitest run src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-redeploy-workflow.test.ts src/test/prod-real-load-steps.test.ts --pool=threads --maxWorkers=2`: passed, 8 tests.

## Objective Mapping

| Objective requirement | Evidence path after this change | Status |
| --- | --- | --- |
| Server pulls/deploys main runtime images | Still requires server-side compose output | Still external |
| Production preflight runs after deploy | Workflow now saves preflight log for bundle input | Bundle-ready |
| Real Backend Test reruns with `pageSize=12` | Bundle rejects summary if list page size is not 12 | Bundle-ready |
| No seed/fixture targets | Bundle rejects summary target paths containing `seed` or `fixture` | Bundle-ready |
| No cache shortcut | Still covered by `prod-real-load-steps.sh` k6 identity suffixes | Covered by runner |
| Result-based next slice selection | Bundle manifest exposes `nextFocus`, `cleanCeilingRps`, and saturation data from real load summary | Bundle-ready |
| Full E2E and CI after selected slice | Still requires future selected slice | Still external |

## Final Recommendation

Use `Production Runtime Redeploy` with `run_real_load=true` after required production SSH/GHCR secrets are configured, or run the scripts manually on the server. The resulting `production-runtime-evidence.tar.gz` should be attached to the next slice decision. Do not mark the active goal complete until that production bundle exists and the result-selected slice has full E2E and CI evidence.
