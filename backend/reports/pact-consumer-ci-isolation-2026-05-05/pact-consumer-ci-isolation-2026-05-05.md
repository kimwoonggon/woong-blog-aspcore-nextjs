# Pact Consumer CI Isolation Audit - 2026-05-05

## Summary
- Fixed the remaining `CI Dev` frontend unit failure in `src/test/pact/public-api-consumer.pact.test.ts`.
- The failure was caused by Pact tests importing public API client modules while module mocks from other Vitest files could still be registered in the same worker.
- Added explicit `vi.doUnmock(...)` cleanup for the public API client modules used by the Pact consumer contracts before dynamic imports and during test cleanup.

## Changed
- `src/test/pact/public-api-consumer.pact.test.ts`
  - Added a public API module allowlist for the Pact consumer test.
  - Added `restorePactTestModules()` to clear Vitest module mocks for those API clients.
  - Called the module restore helper in `beforeEach`, `afterEach`, and `withServerApi()`.
- `todolist-2026-05-05.md`
  - Recorded the second `dev` CI failure, the implementation plan, focused validations, full unit validation, and audit artifact paths.

## Intentionally Not Changed
- Production code was not modified.
- Existing unrelated lint warnings were not changed.
- Existing local backup directories under `.agent-backups/` were not committed.
- The existing Pact contract shape and provider verification flow were not changed.

## Goal Verification
- Goal: unblock `dev` CI before any `main` promotion.
- Result: local frontend unit coverage now passes with the exact CI unit command shape and the previously failing Pact contract file is isolated from unrelated module mocks.
- Non-goal: bypassing or weakening Pact verification. The consumer contracts still execute against Pact mock servers and still verify that expected requests are made.

## Validations
- `npx vitest run src/test/pact/public-api-consumer.pact.test.ts --pool=threads --maxWorkers=1` passed 6/6 tests.
- `npx vitest run src/test/resume-server-render.test.tsx src/test/admin-page-success-states.test.tsx src/test/pact/public-api-consumer.pact.test.ts --pool=threads --maxWorkers=1` passed 32/32 tests.
- `npm run typecheck` passed.
- `npm run lint` passed with the existing six unrelated warnings.
- `npm test -- --run` passed 82/82 test files and 586/586 tests.

## Risks And Follow-Up
- Remote CI must still be re-run after this commit reaches `origin/dev`.
- The lint warnings are pre-existing and remain outside this fix.
- Local full unit execution on WSL is slow, but it completed successfully.

## Recommendation
- Push this fix to `dev`.
- Watch the new `CI Dev` run to completion.
- Promote `dev` to `main` only if the new `dev` run is green.
