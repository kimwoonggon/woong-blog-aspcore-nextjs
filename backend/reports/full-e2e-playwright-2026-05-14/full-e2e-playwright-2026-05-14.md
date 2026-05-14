# Full Playwright E2E Audit - 2026-05-14

## Scope

This audit records the full Playwright E2E suite requested after the earlier targeted E2E validations. The suite was run against the Docker-backed development stack through nginx at `http://127.0.0.1:3000`.

## Command

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npm run test:e2e
```

The npm script resolved to the core profile full suite:

```bash
ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core node scripts/run-e2e-latency.mjs -- --workers=1
```

## Result

Status: failed

- Total tests reported: 435
- Passed: 429
- Failed: 2
- Skipped: 4
- Duration: 14.8 minutes
- Latency artifacts: 435
- Latency budget failures: 0
- Latency warnings: 76
- Summary artifacts:
  - `test-results/playwright/e2e-latency-summary.md`
  - `test-results/playwright/e2e-latency-summary.json`

## Failing Tests

1. `tests/ui-improvement-blog-card-anchors.spec.ts:25`
   - Project: `chromium-public`
   - Title: `blog grid uses three columns at xl breakpoint`
   - Failure: expected 3 distinct first-row column positions, received 1.
   - Primary evidence:
     - `test-results/playwright/ui-improvement-blog-card-a-29130-ee-columns-at-xl-breakpoint-chromium-public/test-failed-1.png`
     - `test-results/playwright/ui-improvement-blog-card-a-29130-ee-columns-at-xl-breakpoint-chromium-public/error-context.md`
     - `test-results/playwright/ui-improvement-blog-card-a-29130-ee-columns-at-xl-breakpoint-chromium-public/video.webm`
     - `test-results/playwright/ui-improvement-blog-card-a-29130-ee-columns-at-xl-breakpoint-chromium-public/trace.zip`

2. `tests/work-inline-create-flow.spec.ts:58`
   - Project: `chromium-authenticated`
   - Title: `existing work video uploads persist thumbnails immediately without requiring Update Work`
   - Failure: `getByAltText('Work thumbnail preview')` was not found within 5000 ms.
   - Primary evidence:
     - `test-results/playwright/work-inline-create-flow-ex-b9844-thout-requiring-Update-Work-chromium-authenticated/test-failed-1.png`
     - `test-results/playwright/work-inline-create-flow-ex-b9844-thout-requiring-Update-Work-chromium-authenticated/error-context.md`
     - `test-results/playwright/work-inline-create-flow-ex-b9844-thout-requiring-Update-Work-chromium-authenticated/video.webm`
     - `test-results/playwright/work-inline-create-flow-ex-b9844-thout-requiring-Update-Work-chromium-authenticated/trace.zip`

## Intentionally Not Changed

- No production code was changed during this validation run.
- No test code was changed during this validation run.
- The two failing tests were not repaired in this audit step; this report captures the requested full-suite result.

## Validation Notes

- Confirmed the Docker compose development stack was already running before the full suite.
- Ran the full configured `npm run test:e2e` Playwright suite with one worker against `http://127.0.0.1:3000`.
- Reviewed the final Playwright failure summary and the generated latency summary.
- Reviewed the Playwright error-context snapshots for the two failures.

## Risks And Yellow Flags

- Full E2E is not green. The current branch should not be reported as having passed full Playwright E2E until the two failures are fixed and the suite is rerun.
- The blog grid failure shows the `/blog` card list rendered as one column at the tested xl breakpoint, which may indicate a layout regression or stale/fixture-dependent CSS/data state.
- The work inline video thumbnail failure shows no `Work thumbnail preview` after immediate video upload persistence, which may indicate an async thumbnail generation, refresh, or editor state update issue.
- The suite produced 76 latency warnings, although no latency budget failures were reported.

## Final Recommendation

Treat the full E2E gate as failed. Fix the two failing specs, then rerun `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npm run test:e2e` before claiming a full Playwright pass.
