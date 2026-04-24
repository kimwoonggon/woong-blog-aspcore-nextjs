# E2E Latency Instrumentation Audit

Date: 2026-04-22

## Summary

Implemented a Playwright latency measurement and budget enforcement layer for the E2E suite.

Key changes:

- Added `tests/helpers/performance-test.ts` as the Playwright fixture wrapper used by root-level specs.
- Added `tests/helpers/latency.ts` for metric collection, budget resolution, `measureStep`, per-test `e2e-latency.json` attachment, and hard-budget enforcement.
- Added `tests/performance-budgets.json` with default measurement budgets and explicit hard budgets for public route load, public pagination/search, admin mutation refresh, editor save/open, and AI dialog/runtime config paths.
- Added `scripts/summarize-e2e-latency.mjs` and `scripts/run-e2e-latency.mjs` to aggregate artifacts and keep summary generation tied to E2E scripts.
- Switched root Playwright specs from `@playwright/test` to `./helpers/performance-test`, preserving helper modules that need direct Playwright types.
- Added `tests/e2e-response-time.spec.ts` and explicit `measureStep` coverage in core Study/Works pagination, Works search, admin page/settings/resume/blog/work mutation flows, and AI dialog readiness.
- Updated E2E package scripts:
  - `test:e2e` runs full one-worker E2E through the latency wrapper.
  - `test:e2e:latency` runs the focused hard-budget latency subset.
  - `test:e2e:exhaustive` runs the full one-worker E2E suite through the same wrapper.

## Intentionally Not Changed

- No production runtime behavior, backend APIs, auth/security semantics, page design, editor UX design, or caching behavior was intentionally changed.
- Heavy media, recording, corpus, visual, and broad UI specs remain measurement-first unless an explicit budget applies.
- The full 577-test suite is still single-worker for this branch; no parallel-worker strategy was introduced.

## Goal Verification

- Every root Playwright spec now imports the performance fixture, so tests that use `page` and/or `request` automatically record:
  - test duration
  - `page.goto` timing
  - document navigation timing
  - tracked `/api/**` and `/revalidate-public` response timing
  - browser interaction timings from Event Timing plus RAF fallback
  - max/slow interaction lists
  - warnings and hard budget failures
- Explicit hard-budget steps were added for the core user-perceived paths.
- `testInfo.attach('e2e-latency.json', ...)` now attaches a path-backed JSON artifact so the summary script can collect it from Playwright output.
- The summary script writes:
  - `test-results/playwright/e2e-latency-summary.json`
  - `test-results/playwright/e2e-latency-summary.md`

## Validation Performed

- `npx vitest run src/test/e2e-latency-budget.test.ts src/test/e2e-latency-summary.test.ts --pool=threads` -> passed, 2 files, 9 tests.
- `npx vitest run src/test/e2e-latency-budget.test.ts src/test/e2e-latency-summary.test.ts src/test/public-admin-rendering.test.tsx --pool=threads` -> passed, 3 files, 15 tests.
- `npm run typecheck` -> passed.
- `npm test -- --run` -> passed, 53 files, 270 tests.
- `npm run build` -> passed.
- `npx eslint src tests playwright.config.ts next.config.ts eslint.config.mjs` -> 0 errors, 3 existing warnings.
- `git diff --check` -> passed.
- `npx playwright test --list` -> 577 tests in 186 files.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npm run test:e2e:latency` -> passed, 12 tests, 12 latency artifacts, 0 budget failures, 0 warnings.
- Full `npm run test:e2e` was exercised against compose:
  - First full run after instrumentation: 566 passed, 3 failed, 8 skipped, 575 latency artifacts, 0 budget failures.
  - The three failures were not latency-budget failures. They were a local-admin shortcut expectation mismatch and an import-rewrite typo in one auth test. Targeted rerun after fixes: 7 passed, 1 skipped.
  - Second full run after auth fixes: 565 passed, 4 failed, 8 skipped, 575 latency artifacts, 0 budget failures.
  - The second run failures were order/timing-sensitive visitor works pagination, home editor reload, renovation home reload, and public loading-state skeleton assertions. Targeted rerun after scoped fixes passed for the affected visitor/home/renovation/loading-state specs.
  - Final full run after all fixes: 569 passed, 8 skipped, 0 failed, 575 latency artifacts, 0 budget failures, 89 warnings.

## Risks And Yellow Flags

- The final full-suite summary contains 89 warnings. They are warning-level latency observations, not hard budget failures.
- Automatic request timing is intentionally broad, but hard failure is limited to explicit budget categories. Non-core heavy specs should be reviewed after collecting several full-run summaries before tightening thresholds.
- Browser interaction timings depend on browser Event Timing support and include RAF fallback measurements; they should be treated as trend data outside explicit `measureStep` budgets.

## Final Recommendation

Keep the latency system and hard-budget subset as implemented. Before merging a performance-sensitive PR, run `npm run test:e2e:latency`. The full 577-test E2E suite is now green against the compose stack with latency artifacts generated.
