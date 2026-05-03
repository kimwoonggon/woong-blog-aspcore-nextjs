# Home Works Mobile Overflow Follow-up Audit - 2026-05-03

## Summary

Fixed the remaining mobile overflow in the home page Works section. The original follow-up issue was not only the outer section rail. A single-column CSS grid without an explicit mobile track could let long unbroken Works content define an oversized implicit grid column. That widened the card and document far beyond the mobile viewport.

## Changed

- Updated `src/app/(public)/page.tsx` so the Works section, grid, card link, card, media, and content stack can shrink with `min-w-0` and `max-w-full`.
- Changed the Works grid to use an explicit mobile `grid-cols-1`, while preserving `md:grid-cols-2` and `xl:grid-cols-3`.
- Added wrapping safeguards for Works category, title, and excerpt using break-word behavior plus `overflow-wrap:anywhere`.
- Added Playwright regressions for:
  - long unbroken Works title/category content on 320px and 375px mobile widths
  - extremely short mobile viewport heights at 320x240 and 375x260 while preserving the same card width

## Intentionally Not Changed

- Did not change Study card layout.
- Did not hide overflow globally on `body` or `html`; the content itself now fits instead.
- Did not introduce viewport-specific card widths or hardcoded rail offsets.
- Did not alter desktop/tablet Works column counts.
- Did not touch unrelated local changes such as `image.png` or prior backup/report directories.

## Goal Verification

- Works cards now remain within the mobile viewport and mobile bottom nav width.
- Card width stays stable when the mobile viewport height is reduced drastically.
- Long unbroken title/category content wraps inside the card instead of widening the grid.
- Study-like mobile fit is achieved without hardcoded width values.

## Validations

- Reproduced the pre-fix overflow with Playwright metrics: injected long unbroken Works content widened `documentElement.scrollWidth` to 2143px at 320px and 375px widths.
- `npx eslint 'src/app/(public)/page.tsx' tests/ui-improvement-featured-works-grid.spec.ts`
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build frontend nginx`
- `curl -I http://127.0.0.1:3000/`
- Verified rendered HTML no longer includes `md:-mx-6` and includes `grid min-w-0 grid-cols-1`.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/ui-improvement-featured-works-grid.spec.ts --project=chromium-public --grep "Works cards do not overflow|Works stays aligned|Works collapses" --workers=1` passed 3/3.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/ui-improvement-featured-works-grid.spec.ts --project=chromium-public --grep "Works card width stays stable" --workers=1` passed 1/1.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/ui-improvement-featured-works-grid.spec.ts --project=chromium-public --workers=1` passed 15/15.
- Manual Playwright metrics after the fix showed document widths equal viewport widths at 320px, 375px, and 768px after injecting long Works content.

## Risks And Follow-up

- The page still depends on real content from the dev backend. The regression tests inject worst-case title/category text in the browser so this specific overflow path is covered regardless of current seed data.
- CI should still be watched after pushing because the repository has branch protection and runtime promotion checks.

## Recommendation

Push this follow-up fix to `dev`, wait for CI, then promote to `main` using the existing main runtime promotion flow.
