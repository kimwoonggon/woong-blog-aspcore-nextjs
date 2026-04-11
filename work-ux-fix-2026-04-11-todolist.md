# Work UX Fix 2026-04-11 Todolist

| ID | Check | Status | Evidence | Notes |
| --- | --- | --- | --- | --- |
| W-1 | public `/works` inline text-only create saves and stays on `/works` | Done | `tests/work-inline-create-flow.spec.ts` | Video: `test-results/playwright/work-inline-create-flow-pu-7fc2b-es-after-a-text-only-create-chromium-public/video.webm` |
| W-2 | public `/works` inline create closes shell and resets form after success | Done | `tests/work-inline-create-flow.spec.ts` | Re-open after success shows empty Title field |
| W-3 | public `/works` inline create with staged videos does not redirect to `/admin/works/{id}` | Done | `tests/work-inline-create-flow.spec.ts` | Video: `test-results/playwright/work-inline-create-flow-pu-711fe-orks-and-refreshes-the-list-chromium-public/video.webm` |
| W-4 | MP4-only staged create persists generated thumbnail | Done | `tests/admin-work-auto-thumbnail.spec.ts` | Existing create lane remains green |
| W-5 | edit-mode MP4 upload persists generated thumbnail immediately | Done | `tests/work-inline-create-flow.spec.ts`, `tests/admin-work-auto-thumbnail.spec.ts` | Video: `test-results/playwright/work-inline-create-flow-ex-b9844-thout-requiring-Update-Work-chromium-public/video.webm` |
| W-6 | edit-mode video changes show explicit “videos save immediately” UX copy | Done | `src/test/work-editor.test.tsx` | Unit coverage added for edit-mode explanatory copy |
| W-7 | video changes are not mistaken for unsaved form edits | Done | `src/test/work-editor.test.tsx` | Video save is immediate; main CTA remains for text/meta/asset edits only |
| W-8 | targeted Playwright runs are recorded and artifact paths are captured | Done | `tests/work-inline-create-flow.spec.ts` | 3 scenario recordings captured under `test-results/playwright/work-inline-create-flow-*` |
| W-9 | single work delete uses lightweight confirm UX instead of typed prompt | Done | `tests/work-single-delete-ux.spec.ts` | Video: `test-results/playwright/work-single-delete-ux-sing-8ddbf--dialog-and-removes-the-row-chromium-public/video.webm` |
