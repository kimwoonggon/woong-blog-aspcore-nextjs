# Production Runtime Evidence Verifier - Audit

Date: 2026-05-11

## Goal

Production SSH and remote execution are out of scope. The goal of this slice is to add a local verifier for evidence returned from the `main` runtime server flow, so Real Backend Test results are not interpreted unless the evidence proves:

- current expected `main` SHA and image digests when supplied
- production preflight passed with nginx/app/gzip/public Work detail contract markers
- Real Backend Test used public HTTPS origin, not backend-direct or localhost
- list targets stayed at `pageSize=12`
- read targets are real public Work/Study detail paths
- no seed/fixture shortcut targets
- failure rate and dropped-iteration thresholds stayed within configured limits

## Changed

- Added `scripts/prod-runtime-evidence-verify.sh`.
- Added behavior tests in `src/test/prod-runtime-evidence-verify.test.ts`.
- Added the verifier to `scripts/main-runtime-allowlist.txt` so it is present in the promoted `main` runtime tree.
- Updated `docs/walkthroughs/main-server-setup.md` with evidence bundle and verifier commands.
- Extended `src/test/prod-runtime-redeploy-workflow.test.ts` to guard the verifier allowlist entry.
- Updated `todolist-2026-05-11.md` with the plan, backup, validation, and progress.

## Intentionally Not Changed

- No production SSH, remote command execution, or secret handling was added.
- No Real Backend Test target was weakened: `pageSize=12`, real read URLs, and no cache shortcut remain required.
- No backend performance code, DTO shape, DB query, HLS pipeline, or Docker runtime behavior was changed.
- No production result was claimed; this is a verifier for evidence after the server-side flow is run by the operator.

## Verification

- RED: `npx vitest run src/test/prod-runtime-evidence-verify.test.ts --pool=threads --maxWorkers=2` failed because `scripts/prod-runtime-evidence-verify.sh` did not exist.
- GREEN focused: same command passed with 1 test.
- Expanded focused: same command passed with 5 tests.
- Related regression: `npx vitest run src/test/prod-runtime-evidence-verify.test.ts src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-runtime-redeploy-workflow.test.ts --pool=threads --maxWorkers=2` passed with 9 tests.
- Shell syntax: `bash -n scripts/prod-runtime-evidence-verify.sh scripts/prod-runtime-evidence-bundle.sh scripts/prod-real-load-steps.sh scripts/prod-runtime-preflight.sh scripts/prod-public-origin-preflight.sh` passed.
- Diff whitespace check for changed files passed.
- `npm run typecheck` passed.
- `npm run lint` passed with 0 errors and 5 pre-existing warnings.

## Risks And Yellow Flags

- This does not replace actual server-side preflight and Real Backend Test execution; it only verifies returned artifacts.
- Expected SHA and image digest checks are optional unless the caller supplies `EXPECTED_MAIN_SHA`, `EXPECTED_BACKEND_IMAGE_DIGEST`, and `EXPECTED_FRONTEND_IMAGE_DIGEST`.
- The verifier checks evidence contract and load hygiene, not backend performance correctness by itself.
- Full E2E was not rerun because this slice changes scripts, docs, and tests only; no app runtime behavior changed.

## Recommendation

Merge this verifier through `dev` and promote it to `main`. When the server operator returns `production-runtime-evidence.tar.gz` or the extracted evidence directory, run `scripts/prod-runtime-evidence-verify.sh` before using Real Backend Test numbers to choose the next backend performance slice.
