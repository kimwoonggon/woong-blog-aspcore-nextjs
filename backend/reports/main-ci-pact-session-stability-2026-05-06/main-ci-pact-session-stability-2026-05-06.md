# Main CI Pact Session Stability - 2026-05-06

## Summary

Main runtime promotion PR #50 passed and merged, but the following main push CI run failed in the frontend unit job. The failing test was the Pact consumer interaction for the unauthenticated session endpoint. This slice keeps production behavior unchanged and stabilizes the Pact consumer test harness.

## Changed

- Added shared Pact test options with `retry: 2` for every interaction in `src/test/pact/public-api-consumer.pact.test.ts`.
- Added `withBrowserApi()` to mirror the existing server-side Pact helper for browser-facing `/api` calls.
- The unauthenticated session Pact test now uses `NEXT_PUBLIC_API_BASE_URL` through the helper and asserts `response.ok` before reading JSON.
- Updated `todolist-2026-05-06.md` with the CI failure evidence, plan alignment, backup path, and validations.

## Intentionally Not Changed

- No production frontend behavior was changed.
- No backend production code was changed.
- No Real Backend Test target selection, seed preference, `pageSize=12`, cache behavior, or load-test scenario semantics were changed.
- No docker, nginx, DB pool, or runtime config was changed in this slice.

## Objective Checklist

| Requirement | Evidence | Status |
| --- | --- | --- |
| Inspect failing main CI | Main push run `25403319917`, job `74508179622`, failed in `contracts unauthenticated session payload` because `GET /api/auth/session` was expected but not received. | Done |
| Keep scope limited to verification blocker | Diff touches only `src/test/pact/public-api-consumer.pact.test.ts`, TODO, and this audit report. | Done |
| Avoid changing load-test semantics | No production backend/load-test files changed. | Done |
| Add focused stability fix | All Pact consumer interactions now share the same retry policy, and session test uses a cleanup helper. | Done |
| Run relevant verification | Focused Pact, related frontend tests, typecheck, lint, and diff whitespace checks passed locally. | Done |
| Produce persistent audit artifacts | Markdown, HTML, and JSON report files were written under this directory. | Done |

## Validations

- PASS: `npm run test -- --run src/test/pact/public-api-consumer.pact.test.ts` - 1 file, 6 tests.
- PASS: `npm run test -- --run src/test/pact/public-api-consumer.pact.test.ts src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts src/test/public-admin-client-gate.test.tsx src/test/public-admin-rendering.test.tsx` - 5 files, 44 tests.
- PASS: `npm run typecheck`.
- PASS: `npm run lint` - 0 errors, 6 existing warnings.
- PASS: `git diff --check`.

## Risks And Follow-Up

- The failing signal was a GitHub Actions full-suite flake and did not reproduce in focused local runs; GitHub Actions remains the authoritative gate.
- If another Pact interaction flakes after this, the next step should be isolating Pact consumer tests into a dedicated serial CI job rather than adding more per-test workarounds.
- Node.js 20 action deprecation warnings remain in CI but are unrelated to this failure.

## Recommendation

Commit this focused CI stability fix, open a PR into `dev`, verify CI, merge to `dev`, then regenerate the runtime-only promotion to `main` and verify the main push CI again.
