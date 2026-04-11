# Work UX Follow-up 2026-04-11 Todolist

| ID | Check | Status | Evidence | Notes |
| --- | --- | --- | --- | --- |
| W2-1 | public `/works` inline create redirects to the new public work detail page | Done | `tests/work-inline-redirects.spec.ts` | Video: `test-results/playwright/work-inline-redirects-publ-45071-new-public-work-detail-page-chromium-public/video.webm` |
| W2-2 | public work detail inline edit redirects back to the edited public work detail page | Done | `tests/work-inline-redirects.spec.ts` | Video: `test-results/playwright/work-inline-redirects-publ-2c928-work-detail-page-after-save-chromium-public/video.webm` |
| W2-3 | video-only changes from public inline edit refresh the public page without requiring main form text edits | Done | `tests/work-inline-redirects.spec.ts` | Video: `test-results/playwright/work-inline-redirects-publ-7b3b5-thout-requiring-Update-Work-chromium-public/video.webm` |
| W2-4 | works cards keep stable height even when title/copy length varies | Done | `src/app/globals.css`, `src/app/(public)/works/page.tsx` | Height stabilized with `works-feed-card`, `works-feed-title`, `works-feed-excerpt` classes |
| W2-5 | targeted Vitest and Playwright runs are recorded and linked | Done | `src/test/work-editor.test.tsx`, `tests/work-inline-redirects.spec.ts` | Work follow-up recordings captured under `test-results/playwright/work-inline-redirects-*` |
| W2-6 | single-item work delete uses simple confirm and removes the row cleanly | Done | `tests/work-single-delete-ux.spec.ts` | Video: `test-results/playwright/work-single-delete-ux-sing-8ddbf--dialog-and-removes-the-row-chromium-public/video.webm` |
