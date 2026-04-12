# Playwright Failure TODO — 2026-04-12

## Context

- Trigger: full admin/public Playwright sweep after docker rebuild
- Base URL used: `http://localhost`
- Command:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost \
npx playwright test \
  tests/admin-*.spec.ts \
  tests/public-*.spec.ts \
  tests/ui-admin-*.spec.ts \
  tests/ui-improvement-*.spec.ts \
  tests/home.spec.ts \
  tests/introduction.spec.ts \
  tests/resume.spec.ts \
  tests/auth-login.spec.ts \
  tests/auth-security-browser.spec.ts \
  tests/dark-mode.spec.ts \
  tests/manual-qa-gap-coverage.spec.ts \
  tests/manual-qa-auth-gap.spec.ts \
  tests/work-*.spec.ts \
  --workers=1
```

- Result: `224 passed`, `28 failed`
- Artifact root: `test-results/playwright/`

## Priority 1 — Real Regression Or Runtime Behavior Mismatch

- [x] `tests/work-inline-create-flow.spec.ts:9`
  - `public works inline create saves in place and closes after a text-only create`
- [x] `tests/work-inline-create-flow.spec.ts:31`
  - `public works inline create with staged videos stays on /works and refreshes the list`
- [x] `tests/work-inline-create-flow.spec.ts:58`
  - `existing work video uploads persist thumbnails immediately without requiring Update Work`
- [x] `tests/work-inline-redirects.spec.ts:47`
  - `public work detail video-only edits enable Update Work without requiring body changes`
- [x] `tests/admin-blog-image-upload.spec.ts:7`
  - `blog editor uploads an inline image and public blog renders it`
- [x] `tests/admin-blog-publish.spec.ts:6`
  - `admin can create and publish a blog post that appears on public blog page`
- [x] `tests/admin-work-image-upload.spec.ts:7`
  - `work editor uploads thumbnail/icon media and reuses them across admin/public views`
- [x] `tests/admin-work-publish.spec.ts:6`
  - `admin can create and publish a work that appears on public works page`
- [x] `tests/resume.spec.ts:6`
  - `resume page exposes a download action`
- [x] `tests/manual-qa-gap-coverage.spec.ts:361`
  - `C-8 HTML widget renders through the public interactive renderer`

## Priority 2 — Tests Broken By Intentional UI Changes

- [x] `tests/admin-search-pagination.spec.ts:5`
  - blog page still expects `First/Last` pagination controls
- [x] `tests/admin-search-pagination.spec.ts:38`
  - works page still expects `First/Last` pagination controls
- [x] `tests/ui-admin-table-search.spec.ts:5`
  - blog table filter test likely uses old tag column indexing / row assumptions
- [x] `tests/ui-admin-table-search.spec.ts:28`
  - works table filter test likely uses old category column indexing / row assumptions
- [x] `tests/admin-work-special-input.spec.ts:6`
  - still expects old create-mode helper copy: `New works publish immediately when you save.`
- [x] `tests/public-inline-editors.spec.ts:22`
  - still expects old public inline create affordance label `새 글 쓰기`
- [x] `tests/public-admin-affordances.spec.ts:16`
  - still expects old public blog create affordance label `새 글 쓰기`

## Resolved In Current Session

- [x] Public inline work create/create-with-videos flow now closes correctly and keeps the intended list/detail behavior.
- [x] Public blog admin affordance labels were normalized to Korean public-admin wording for consistency with the rest of the public inline tools.
- [x] `tests/helpers/auth.ts` now uses a more stable navigation helper to reduce local dev `ERR_ABORTED` flake.
- [x] Verification:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost:3000 \
npx playwright test \
  tests/work-inline-create-flow.spec.ts \
  tests/work-inline-redirects.spec.ts \
  tests/public-inline-editors.spec.ts \
  tests/public-admin-affordances.spec.ts \
  --workers=1
```

  - Result: `13 passed`

- [x] Admin table/search/create-copy regressions were updated to the current UI contract.
- [x] Admin publish/upload checks now use deterministic public verification paths.
- [x] Verification:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost:3000 \
npx playwright test \
  tests/admin-search-pagination.spec.ts \
  tests/ui-admin-table-search.spec.ts \
  tests/admin-work-special-input.spec.ts \
  tests/admin-blog-publish.spec.ts \
  tests/admin-work-publish.spec.ts \
  tests/admin-blog-image-upload.spec.ts \
  tests/admin-work-image-upload.spec.ts \
  --workers=1
```

  - Result: all targeted reruns passed

- [x] Delete/reorder/manual-gap tests were updated to the current in-app dialog and icon-button interactions.
- [x] Verification:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost:3000 \
npx playwright test \
  tests/work-single-delete-ux.spec.ts \
  tests/manual-qa-gap-coverage.spec.ts \
  -g "A-6|A-12|B-5|single work delete" \
  --workers=1
```

  - Result: `4 passed`

- [x] Resume download flow is green on the latest local code path.
- [x] Dark-mode regression expectations were aligned to the current semantic-token UI contract.
- [x] Verification:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost:3000 \
npx playwright test tests/resume.spec.ts --workers=1

PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost:3000 \
npx playwright test tests/dark-mode.spec.ts -g "DM-08|DM-10|DM-11|DM-15|DM-16|DM-18" --workers=1
```

  - Result: targeted reruns passed

## Priority 3 — Tests Broken By Delete / Reorder Interaction Changes

- [x] `tests/work-single-delete-ux.spec.ts:5`
  - still expects browser `confirm`-style delete flow
- [x] `tests/manual-qa-gap-coverage.spec.ts:140`
  - `A-6 reorder saved work videos changes public playback order`
  - likely still expects text buttons like `Move Down`
- [x] `tests/manual-qa-gap-coverage.spec.ts:173`
  - `A-12 single work delete removes the work from admin and public lists`
- [x] `tests/manual-qa-gap-coverage.spec.ts:190`
  - `B-5 single blog delete removes the post from admin and public lists`
- [x] `tests/manual-qa-gap-coverage.spec.ts:440`
  - `F-5 and F-6 inline create flows can create works and blog posts from public pages`
  - verify whether failures are button-label drift, close-shell behavior, or save response timing

## Priority 4 — Dark Mode / Visual Expectation Drift

- [x] `tests/dark-mode.spec.ts:90`
  - `DM-08 work detail page uses navy date badges and orange accents in dark mode`
- [x] `tests/dark-mode.spec.ts:113`
  - `DM-10 blog detail page uses navy date badges and keeps prose readable`
- [x] `tests/dark-mode.spec.ts:131`
  - `DM-11 contact page email link uses the dark blue variant`
- [x] `tests/dark-mode.spec.ts:162`
  - `DM-18 prose code blocks use the darker code block background`
  - strict locator issue also needs test cleanup
- [x] `tests/dark-mode.spec.ts:268`
  - `DM-15 blog editor uses the dark brand button`
- [x] `tests/dark-mode.spec.ts:279`
  - `DM-16 work editor uses the dark brand button`

## Suggested Repair Order

- [x] Update stale tests for pagination, labels, and create-mode helper copy
- [x] Update stale tests for dialog/reorder controls
- [x] Re-run targeted admin/public inline create and publish/upload flows
- [x] Fix remaining runtime regressions:
  - `tests/manual-qa-gap-coverage.spec.ts:371` (`C-8 HTML widget`)
  - `tests/work-inline-redirects.spec.ts:8` (public inline create page-context expectation differs on docker front door)
  - `tests/resume.spec.ts:6` (resume upload state not observed within timeout on docker front door)
- [x] Revisit dark-mode expectations and decide whether code or tests should move
- [x] Re-run full sweep and collapse this file into the main QA ledger once green

## Latest Full Sweep

- Command:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost \
npx playwright test \
  tests/admin-*.spec.ts \
  tests/public-*.spec.ts \
  tests/ui-admin-*.spec.ts \
  tests/ui-improvement-*.spec.ts \
  tests/home.spec.ts \
  tests/introduction.spec.ts \
  tests/resume.spec.ts \
  tests/auth-login.spec.ts \
  tests/auth-security-browser.spec.ts \
  tests/dark-mode.spec.ts \
  tests/manual-qa-gap-coverage.spec.ts \
  tests/manual-qa-auth-gap.spec.ts \
  tests/work-*.spec.ts \
  --workers=1
```

- Result: near-complete full sweep reached `251 passed`, `1 flaky failure` on `D-6 stale editor sessions`
- Follow-up:
  - `tests/manual-qa-auth-gap.spec.ts:20` was hardened to wait for the actual login heading before asserting the redirect URL.
  - Targeted rerun passed:

```bash
PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost \
npx playwright test tests/manual-qa-auth-gap.spec.ts -g "D-6 stale editor sessions" --workers=1
```
