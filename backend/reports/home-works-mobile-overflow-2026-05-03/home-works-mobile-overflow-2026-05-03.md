# Home Works Mobile Overflow Audit - 2026-05-03

## Summary

Fixed the mobile home page Works section so it no longer expands past the home content rail. The change removes negative horizontal margin compensation from the featured Works section, so it follows the parent content rail naturally instead of countering parent padding with breakpoint-specific offsets.

## Changed

- Updated `src/app/(public)/page.tsx`:
  - Removed negative horizontal margin compensation from `featured-works-section`.
- Updated `tests/ui-improvement-featured-works-grid.spec.ts`:
  - Added a mobile Playwright regression asserting the Works section and first card stay within the home content rail at 375px width.
  - Named the representative phone viewport fixture instead of repeating raw viewport numbers in the new and adjacent mobile tests.
- Updated `todolist-2026-05-03.md`:
  - Preserved the existing GPT-5.5 TODO section and appended this task's TODO/progress notes.

## Intentionally Not Changed

- Did not alter the Works card component structure, image ratio, typography, or hover behavior.
- Did not change Study Notes or other home sections.
- Did not change navbar sizing or mobile bottom navigation behavior.
- Did not remove or modify unrelated existing worktree changes such as `image.png` and prior report/backup directories.

## Goal Verification

- Goal: stop the mobile home Works card area from extending beyond the navigation/content width.
- Result: the featured Works section now respects the mobile home container rail, and the new Playwright regression checks section/card bounds at mobile width.

## Validations

- `npx eslint 'src/app/(public)/page.tsx' tests/ui-improvement-featured-works-grid.spec.ts` passed.
- `docker compose -f docker-compose.dev.yml up -d --build` built backend/frontend images and completed frontend `next build`.
- Default compose port publishing hit Docker forwarding errors on `127.0.0.1:8080` and `127.0.0.1:3001`; reran with `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002`.
- `curl -I http://127.0.0.1:3000` returned `200 OK`.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/ui-improvement-featured-works-grid.spec.ts --project=chromium-public --grep "Works stays aligned|Works collapses" --workers=1` passed after removing all negative margin compensation: 2 tests.

## Risks And Follow-Up

- The initial red Playwright run could not reach the layout assertion because the standalone dev server expected a backend on `127.0.0.1:8080`; compose validation covered the final behavior.
- Docker port forwarding for default `8080` and `3001` failed in this environment, so the running compose stack uses backend `18080` and HTTPS `3002`.

## Recommendation

Keep the scoped CSS fix and regression test. No broader layout refactor is needed for this issue.
