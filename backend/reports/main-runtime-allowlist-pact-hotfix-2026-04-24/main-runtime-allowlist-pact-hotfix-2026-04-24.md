# Main Runtime Allowlist And Pact Hotfix Audit

## Summary
- Fixed `scripts/main-runtime-allowlist.txt` so runtime-only `main` promotions include CI-critical helper and script files.
- Added `tests/performance-budgets.json` to the allowlist to satisfy `e2e-latency-budget` unit tests on `main`.
- Fixed `src/test/pact/public-api-consumer.pact.test.ts` by mocking `@/lib/api/public-server` directly, preventing Next request-scope errors in Pact consumer tests.

## Intentionally Not Changed
- Did not change production runtime behavior.
- Did not change backend API contracts.
- Did not clean local Playwright artifact noise in the working tree.

## Goal Check
- Goal: repair `main` runtime promotion omissions. Met.
- Goal: repair the Pact consumer test failure exposed in `main` unit tests. Met.

## Validations Performed
- `gh run view 24893006800 --log-failed`
- `npx vitest run src/test/pact/public-api-consumer.pact.test.ts src/test/e2e-latency-budget.test.ts`

## Risks / Yellow Flags
- Future `main` runtime failures can recur if new CI-referenced files are introduced without updating `scripts/main-runtime-allowlist.txt`.
- This patch fixes the Pact helper mock for current `public-server` resolution; if API base URL resolution changes again, the Pact helper may need updates.

## Final Recommendation
- Keep `main-runtime-allowlist.txt` aligned with every file referenced by `CI Main Runtime` and its unit tests.
