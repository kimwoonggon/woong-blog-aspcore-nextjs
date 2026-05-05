# Pact and Responsive Vitest Stability - 2026-05-06

## Summary
PR #45 was blocked by the frontend CI gate after the backend heavy-detail optimization branch passed backend, compose, and provider checks. The failing CI evidence was a Pact consumer mismatch where `/api/auth/session` was expected but not observed by the Pact mock server.

This slice stabilizes the frontend verification gate without changing production/backend behavior.

## Changed
- Enabled Vitest `unstubGlobals` and `unstubEnvs` so global `fetch` and environment stubs are restored between tests.
- Updated the `PublicResponsiveFeed` Study restore test to wait for both scroll restore calls, including the `requestAnimationFrame` callback, so the callback cannot leak into the next test.
- Updated the 2026-05-06 TODO with the PR #45 CI blocker and validation evidence.

## Intentionally Not Changed
- No backend production code was changed in this slice.
- No Real Backend Test target semantics were changed: no seeded priority and no `pageSize=1` shortcut.
- No cache behavior was introduced.
- Existing unrelated lint warnings were not addressed.

## Goal Verification
- PR #45 frontend CI blocker addressed: Vitest now restores global/env stubs between tests, targeting the observed Pact mock-server request-missing failure mode.
- Local responsive-feed flake addressed: the test now drains the scheduled scroll restore before the next test can install a new `scrollTo` spy.
- Backend optimization branch remains scoped: production changes from the prior commit are untouched.

## Validations
- PASS: `npm run test -- --run src/test/pact/public-api-consumer.pact.test.ts` (1 file, 6 tests).
- PASS: `npm run test -- --run src/test/public-responsive-feed.test.tsx` (1 file, 17 tests).
- PASS: `npm run typecheck`.
- PASS: `npm run lint` (0 errors, 6 existing warnings).
- PASS: `npm run test -- --run` (82 files, 589 tests).
- PASS: `git diff --check`.

## Risks and Follow-Up
- CI still needs to be rerun on GitHub after this commit is pushed.
- The frontend suite still emits unrelated React act and console warnings; they are not current blockers but should be reduced later.
- The main backend performance follow-up remains heavy detail read iteration 2: stored representative image model, public/admin content split, DB roundtrip/timing instrumentation, and JSON allocation reduction.

## Recommendation
Commit and push this CI-stability slice to PR #45, watch CI, then merge to `dev` if all checks pass.
