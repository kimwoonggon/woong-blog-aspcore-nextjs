# Home Works Responsive Grid Fix Audit - 2026-05-12

## Scope

Fix the public home Works section so it matches the requested responsive policy, and separate Work/Study detail reading content from the beige/off-white light-mode page background.

## User Goals

- Home Works desktop: up to 8 items, 2 rows x 4 columns.
- Home Works mobile: one card wide, show up to 4 cards so the section does not feel like it has empty grid slots.
- Keep the overall light-mode page background, but make Work/Study detail reading content white.

## Changed

- `HomeQueryStore.GetFeaturedWorksAsync` now returns up to 8 published works instead of 4.
- Home page slices featured works to 8 before rendering.
- Home Works grid now uses 1 column on mobile, 2 columns on tablet, and 4 columns on desktop.
- Home Works cards after the fourth are hidden below the `md` breakpoint, leaving mobile with at most 4 visible cards.
- Blog and Work detail body containers now use a white `bg-card` reading surface with border, radius, padding, and shadow.
- Added/updated tests for backend 8-item home payload, responsive home Works layout, and white detail reading surfaces.

## Intentionally Not Changed

- Did not change the global light-mode page background token.
- Did not change public Works list pagination or load-test `pageSize=12` policy.
- Did not add cache behavior or seed-based load-test shortcuts.
- Did not use production SSH or production deployment commands.
- Did not change Work/Study public list DTO contracts.

## Goal Verification

- Desktop Works grid has 4 columns at desktop width.
- Tablet Works grid remains 2 columns.
- Mobile Works grid remains 1 column and only 4 cards are visible while the DOM can contain 8 cards.
- Backend home payload can provide 8 featured works for the desktop grid.
- Blog and Work detail reading bodies are white in light mode while the surrounding page atmosphere remains unchanged.

## Validations

- RED backend test failed before implementation: expected 8 featured works but actual was 4.
- RED browser test failed before detail-surface implementation: detail body background was transparent.
- `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~GetHomeQueryHandler_ReturnsUpToEightFeaturedWorks_ForBalancedDesktopHomeGrid" --logger "console;verbosity=minimal"` passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13000 npx playwright test tests/ui-improvement-featured-works-grid.spec.ts --project=chromium-public --grep "eight DOM cards|two columns on tablet and four on desktop" --workers=1` passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13000 npx playwright test tests/ui-quality-blog-detail-visuals.spec.ts --project=chromium-authenticated --grep "white reading surface" --workers=1` passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13000 npx playwright test tests/public-detail-toc-fallback.spec.ts tests/public-blog-toc-layout.spec.ts tests/public-work-toc.spec.ts --project=chromium-public --workers=1` passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13000 npx playwright test tests/ui-quality-responsive-admin.spec.ts --project=chromium-authenticated --workers=1` passed.
- `npm run typecheck` passed.
- `npm run lint` passed with 5 existing warnings and 0 errors.
- `git diff --check` passed before report generation.

## Environment Notes

- Docker dev stack was validated on `http://127.0.0.1:13000` with `BACKEND_PUBLISH_PORT=18080`, `NGINX_HTTP_PORT=13000`, `NGINX_HTTPS_PORT=13001`, `NGINX_DEFAULT_CONF=./nginx/default.conf`, and `POSTGRES_DATA_DIR=/tmp/woong-blog-dev-pg-home-works-grid`.
- The alternate ports avoided an existing local `8080` bind conflict.
- The `/tmp` Postgres data directory avoided DrvFS permission failures from bind-mounted database data.
- HTTP-only nginx config avoided missing local TLS certificate files in this worktree.

## Risks And Follow-Up

- The 5 lint warnings are pre-existing warnings in unrelated test/script files; no new lint errors were introduced.
- Mobile currently keeps hidden cards in the DOM. This is intentional to keep server-rendered markup stable while limiting visible mobile cards; if payload size becomes a concern, a future client-side responsive fetch policy could be considered.
- The detail reading surface reduces inner text width by adding padding inside the same max-width container. TOC and related-layout regressions passed, but screenshots may still be useful before a visual release.

## Recommendation

Ship this slice through `dev` after CI passes. It directly addresses the homepage Works empty-slot issue and the Work/Study reading-surface issue without changing global light-mode background or production behavior.
