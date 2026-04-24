# Playwright Notion Isolation Audit

Date: 2026-04-22

## Summary

This focused infrastructure/testing change reduces Playwright 4-worker collisions by making Notion-related blog mutation specs operate on test-owned blog fixtures instead of ambient/default blog rows.

## Changed

- Added `tests/helpers/content-fixtures.ts` with `createBlogFixture()`.
- Updated `tests/admin-blog-edit.spec.ts`:
  - The regular blog edit test now creates and edits its own blog instead of editing the first admin row.
  - The Notion list/autosave test now creates two owned blogs, opens one by id, selects the other by title, and autosaves only that owned document.
- Updated `tests/ui-admin-notion-autosave-info.spec.ts`:
  - Autosave success, autosave failure, and document info tests each create/open an explicit blog id.
- Updated `tests/ui-admin-notion-visual-state.spec.ts`:
  - Highlight and save-state visual tests create/open explicit blog ids.
- Updated `tests/ui-admin-semantic-colors.spec.ts`:
  - The Notion save-state section uses an owned blog fixture instead of the default first document.
- Added grounded 60-second timeouts for Notion-heavy specs because the autosave UI intentionally waits through debounce/network states and was timing out under 4-worker diagnostic load.

## Root Causes Addressed

- Notion tests were mutating the default/first global blog document.
- `admin-blog-edit` edited the first admin blog row, which can be a freshly generated baseline blog under parallel execution.
- Baseline generation specs and Notion/admin edit specs were sharing the same global blog list without ownership boundaries.

## Intentionally Not Changed

- No product code was changed.
- No backend APIs were changed.
- No assertions were removed.
- No retries were added.
- Resume PDF SSR work was not bundled here.
- Broader singleton-state collisions in resume/site settings/pages/work media remain separate follow-up work.

## Validations

- Serial target group:
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/admin-blog-edit.spec.ts tests/ui-admin-notion-autosave-info.spec.ts tests/ui-admin-notion-visual-state.spec.ts tests/ui-admin-semantic-colors.spec.ts --workers=1`
  - Result: 8 passed.
- Parallel diagnostic:
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/admin-blog-edit.spec.ts tests/ui-admin-notion-autosave-info.spec.ts tests/ui-admin-notion-visual-state.spec.ts tests/ui-admin-semantic-colors.spec.ts tests/plain-baseline-0419.spec.ts --workers=4`
  - Before fix: 1 baseline collision failure where `admin-blog-edit` mutated a `plain-baseline` generated blog.
  - After fix and timeout tuning: 48 passed.
- `npm run lint` -> passed with 5 existing warnings.
- `npm run typecheck` -> passed.
- Full Playwright single-worker:
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test --workers=1`
  - First result after Notion isolation: 561 passed, 2 failed, 8 skipped.
  - Failed lines were visual measurement flakes:
    - `tests/ui-improvement-container-width.spec.ts:47`
    - `tests/ui-quality-visual-contracts.spec.ts:73`
  - Test code was updated to wait for valid geometry before asserting.
  - Final result: 563 passed, 8 skipped.
  - Final `npm run lint` -> passed with 5 existing warnings.
  - Final `npm run typecheck` -> passed.

## Remaining Risks

- This only addresses the Notion/default-blog and first-admin-row collision class.
- Full-suite 4-worker stability can still fail on other known singleton/global-state areas: resume/site settings/pages, work media thumbnail ordering, first-row delete flows, and shared authenticated storage.
- Full-suite 1-worker is clean after stabilizing two visual geometry assertions.
- The helper currently creates fixture records but does not delete them after tests. That is intentional for preserving public/admin assertions, but future cleanup may be useful once all tests stop depending on global list order.

## Recommendation

Proceed with this as the first Playwright isolation commit. Next focused slice should isolate work/media thumbnail tests from unfiltered `/works` ordering or snapshot/restore singleton resume/page/site-settings tests.
