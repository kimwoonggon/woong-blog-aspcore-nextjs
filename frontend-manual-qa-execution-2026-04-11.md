# Frontend Manual QA Execution Log 2026-04-11

## Environment
- Base URL: `http://localhost`
- Runtime: local docker-compose stack via nginx/frontend/backend/db
- Browser lane: Playwright Chromium
- Video artifacts: enabled by Playwright config

## Completed Automated Runs
- `npm run test:e2e:public`
  Result: `21 passed`
- `npm run test:e2e:admin`
  Result: `17 passed`
- `npm run test:e2e:works`
  Result: `19 passed`
- `npm run test:e2e:blog`
  Result: `11 passed`
- `npm run test:e2e:uploads`
  Result: `10 passed`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/admin-ai-batch-jobs.spec.ts tests/public-inline-editors.spec.ts tests/public-admin-affordances.spec.ts tests/auth-login.spec.ts --workers=1`
  Result: `8 passed`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/auth-security-browser.spec.ts --workers=1`
  Result: `2 passed`

## Gap Coverage Runs
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/manual-qa-gap-coverage.spec.ts tests/manual-qa-auth-gap.spec.ts --workers=1`
  Result: `9 passed`, `8 failed`
- Focused rerun
  Command: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/manual-qa-gap-coverage.spec.ts --workers=1 --grep "A-6|C-1|C-3|C-4|C-5|C-8|C-10|F-5|G-5"`
  Result: `6 passed`, `4 failed`
- Individual reruns
  - `A-6 reorder` -> passed after backend reorder fix + compose rebuild
  - `C-1/C-2/C-7 formatting/link/code` -> passed in isolated rerun
  - `C-3 drag-drop image` -> passed after editor drop handling fix + compose rebuild
  - `C-10 bubble menu` -> passed after aligning browser selection setup with the current floating toolbar behavior

## Confirmed Passing Gap Scenarios
- `A-6` reorder saved work videos changes public playback order
- `A-12` single work delete
- `B-5` single blog delete
- `C-1/C-2/C-7` formatting, link, and code block render publicly
- `C-3` drag-drop image upload
- `C-4` paste image upload
- `C-5` duplicate work video embed prevention
- `C-6` slash command insertion
- `C-8` HTML widget render
- `C-9` Three.js block render
- `C-11` editor content sync after reload
- `D-2` local admin login
- `D-5` CSRF enforcement
- `F-3` introduction inline save
- `F-4` contact inline save
- `F-5` inline work create
- `F-6` inline blog create
- `G-5` mobile public navigation
- `H-5` very long body render

## Flaky / Order-Sensitive Results
- `A-12` single work delete
  Passed in isolated gap run, but failed inside one large aggregate run
- `C-1/C-2/C-7` toolbar formatting, link, and code block render publicly
  Failed inside the large aggregate run, then passed when rerun in isolation

## Fixes Applied
- Backend `WorkVideoService.ReorderAsync` now uses a two-phase sort-order update to avoid the `(WorkId, SortOrder)` unique index collision during swaps.
- Public work detail sorts videos by `sortOrder` before rendering.
- Tiptap editor wrapper now handles image file drops at the container level in addition to the editor-level drop handler.
- Tiptap floating-format toolbar now listens to browser selection changes directly, and the `C-10` browser scenario now passes when selection is created via DOM range in Playwright.

## Partial / Not Fully Automatable In Current Session
- `D-1` Google login full completion
  CTA and redirect are automated; full provider completion is not
- `D-6` stale session save UX
  Session invalidation is automated; final user-facing guidance still needs human confirmation
- `H-4` empty works list
  Not run automatically because it requires destructive dataset cleanup

## Artifact Locations
- Latest artifact summary: [test-results/playwright/summary/latest-upload-artifacts.md](/mnt/d/woong-blog/woong-blog/test-results/playwright/summary/latest-upload-artifacts.md)
- Current video count in `test-results/playwright`: `27`
- Key failure video folders:
  - [A-6](/mnt/d/woong-blog/woong-blog/test-results/playwright/manual-qa-gap-coverage-A-6-351af-anges-public-playback-order-chromium-public)
  - [C-1/C-2/C-7](/mnt/d/woong-blog/woong-blog/test-results/playwright/manual-qa-gap-coverage-C-1-5a925--code-block-render-publicly-chromium-public)
  - [C-3](/mnt/d/woong-blog/woong-blog/test-results/playwright/manual-qa-gap-coverage-C-3-6f8e3--an-image-into-blog-content-chromium-public)
  - [C-10](/mnt/d/woong-blog/woong-blog/test-results/playwright/manual-qa-gap-coverage-C-1-31e0c-ext-reveals-the-bubble-menu-chromium-public)
