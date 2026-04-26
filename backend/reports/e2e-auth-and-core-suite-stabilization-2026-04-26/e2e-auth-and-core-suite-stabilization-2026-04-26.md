# E2E Auth and Core Suite Stabilization Audit

Date: 2026-04-26

## Summary

Implemented the E2E stabilization plan for the current codebase. `npm run test:e2e` now runs the release-blocking core lane, optional recording/live/batch/manual-heavy specs are split behind explicit profiles, authenticated admin tests refresh `test-login` sessions per test, and public detail/TOC/mutable-content assumptions were updated to match the current UI and content model.

The core lane passed after implementation against a fresh local full-stack dev endpoint at `http://127.0.0.1:3001`: 410 passed, 5 skipped, 0 failed, 0 latency budget failures, 22.1 minutes.

## Changed

- Added `ensureAdminSession(page, returnPath?)` and `ensureAdminApiContext(request, returnPath?)` in the Playwright auth harness.
- Refreshed admin sessions at the start of each `chromium-authenticated` test while leaving runtime auth/negative auth coverage outside that auto-refresh path.
- Changed content fixture helpers to create blogs and works through a test-login-authenticated admin API context, then poll until published fixtures are readable through public APIs.
- Split Playwright execution into `core`, `optional`, and `exhaustive` profiles.
- Updated package scripts so `npm run test:e2e` means the core release-blocking lane, with `test:e2e:optional` and `test:e2e:exhaustive` available separately.
- Added public detail title test IDs for blog/work titles and a work-specific TOC test ID.
- Updated public detail, TOC, inline-edit, search, visual, and mobile navigation specs to avoid stale selectors, mutable seeded copy, and first-card assumptions.
- Added localhost/127.0.0.1 guard logic for server-side QA query flags and matching Playwright skip helpers.
- Fixed stale CSS assertion in the font-loading test so it accepts formatted `font-display: swap`.

## Intentionally Not Changed

- No production API contract was changed.
- The `test-login` behavior was not broadened for real public production use.
- `global-setup` storage-state bootstrap remains in place as a bootstrap optimization, not as the sole authentication guarantee.
- Optional recording/live/batch/manual-heavy specs were not deleted; they were separated from release-blocking core.
- Existing unrelated dirty worktree changes were not reverted or normalized.

## Validation

- `npm run typecheck`: passed after final changes.
- `npm test -- src/test/table-of-contents.test.tsx src/test/local-qa-query-boundary.test.tsx`: passed, 2 files / 12 tests.
- Playwright profile listing:
  - core: 415 tests in 172 files.
  - optional: 174 tests in 17 files.
  - exhaustive: 589 tests in 189 files.
- Targeted public detail/TOC validation on fresh port 3001: passed after cold-route rerun.
- Targeted admin smoke: `admin-dashboard`, `admin-menus`, `admin-members` passed 8/8.
- Targeted Notion group: `ui-admin-notion-autosave-info`, `ui-admin-notion-library-sheet`, `ui-admin-notion-client-switch` passed 11/11.
- Targeted works video group passed after rerunning one transient Next dev failure.
- `admin-auth-authorization` passed under `chromium-runtime-auth`, confirming negative auth coverage is not auto-admined.
- Earlier-failure regression set passed 12/12 across public, authenticated, and runtime-auth projects.
- Full core lane: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3001 npm run test:e2e -- --reporter=list` passed with 410 passed, 5 skipped, 0 failed, latency artifacts 414, budget failures 0, warnings 90.

## Risks and Follow-Up

- Docker production-like containers were already running but were built before the final edits. A meaningful production-like release validation should rebuild that stack and rerun `npm run test:e2e` against it.
- The full core run used the local dev endpoint rather than the rebuilt Docker production-like endpoint.
- The core lane still has 5 intentional skips from existing tests; this report did not convert or remove those skips.
- Latency warnings remain informational; no hard latency budget failures were present in the final core run.
- Post-audit correction: an existing nginx container was exposing host ports 80 and 443, which does not match the requested Docker test boundary. It was stopped after review; future Docker validation must use HTTP on `127.0.0.1:3000` only.

## Recommendation

Use the new core lane as the release-blocking E2E signal. Before an actual production promotion, rebuild the Docker test stack with the frontend bound only to `http://127.0.0.1:3000` and rerun `npm run test:e2e` once against that endpoint to confirm parity with the green local core result.
