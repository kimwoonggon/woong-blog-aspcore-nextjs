# Production Runtime Preflight Public DTO Gate - Audit

Date: 2026-05-11

## Goal

Strengthen the production evidence chain so stale public Work list DTO responses cannot pass preflight/evidence verification and cannot be used to select the next Real Backend Test performance slice.

## Changed

- `scripts/prod-runtime-preflight.sh`
  - Requires the public Work list probe to report `pageSize=12`.
  - Rejects stale Work list DTO keys: `iconUrl`, `period`, `contentJson`, `originalFileName`, `fileSize`, `createdAt`.
  - Emits `public Work list contract: current` only after the list contract is verified.
- `scripts/prod-runtime-evidence-bundle.sh`
  - Requires the preflight log to contain `public Work list contract: current`.
- `scripts/prod-runtime-evidence-verify.sh`
  - Requires returned evidence to prove `public Work list contract: current`.
- Tests updated/added:
  - `src/test/prod-runtime-preflight.test.ts`
  - `src/test/prod-runtime-evidence-bundle.test.ts`
  - `src/test/prod-runtime-evidence-verify.test.ts`

## Intentionally Not Changed

- No production SSH or remote production command execution.
- No Real Backend Test target weakening.
- No cache shortcut.
- No backend DTO/query/schema/HLS behavior change.
- No server deploy was claimed.

## Verification

- RED 1: `npx vitest run src/test/prod-runtime-preflight.test.ts --pool=threads --maxWorkers=2` failed because stale public Work list fields were accepted.
- GREEN 1: same command passed, 7 tests passed.
- RED 2: `npx vitest run src/test/prod-runtime-evidence-verify.test.ts src/test/prod-runtime-evidence-bundle.test.ts --pool=threads --maxWorkers=2` failed because missing public Work list contract evidence was accepted.
- GREEN focused: `npx vitest run src/test/prod-runtime-preflight.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-runtime-evidence-bundle.test.ts --pool=threads --maxWorkers=2` passed, 17 tests passed.
- Related regression: `npx vitest run src/test/prod-runtime-preflight.test.ts src/test/prod-runtime-evidence-verify.test.ts src/test/prod-runtime-evidence-bundle.test.ts src/test/prod-public-origin-preflight.test.ts src/test/prod-real-load-steps.test.ts src/test/prod-runtime-redeploy-workflow.test.ts --pool=threads --maxWorkers=2` passed, 29 tests passed.
- Shell syntax: `bash -n scripts/prod-runtime-preflight.sh scripts/prod-runtime-evidence-bundle.sh scripts/prod-runtime-evidence-verify.sh scripts/prod-public-origin-preflight.sh scripts/prod-real-load-steps.sh` passed.
- Diff whitespace check passed.
- `npm run typecheck` passed.
- `npm run lint` passed with 0 errors and 5 existing warnings.

## Risks And Yellow Flags

- This only strengthens evidence gating. It does not deploy the production server.
- A fresh server-side evidence bundle is still required before selecting the next performance slice.
- The current public origin was previously observed as stale, so production Real Backend Test results must remain blocked until this gate passes on returned evidence.

## Recommendation

Merge this guard through `dev` and promote it to `main`. Then require returned production evidence to pass `prod-runtime-evidence-verify.sh` before interpreting Real Backend Test results.
