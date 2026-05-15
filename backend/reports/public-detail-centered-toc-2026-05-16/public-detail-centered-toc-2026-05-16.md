# Public Detail Centered TOC And Light Surface Audit - 2026-05-16

## Scope

This audit covers the follow-up Work/Study public detail UI change requested after the widened `On This Page` rail: keep the wider rail, make light-mode base/card/background surfaces white, and keep the main Work/Study article body centered independently of the rail.

## Changed

- Re-centered Blog and Work detail article bodies by changing the desktop detail grid to symmetric side columns.
- Kept the TOC rail visually detached on the right with a viewport-aware `clamp(13rem, calc(50vw - 27rem), 20rem)` width.
- Set light-mode base background to pure white and made detail TOC, related shells, related cards, pagination chrome, and previous/next cards white in light mode.
- Preserved dark-mode contrast by using `dark:bg-card` or `dark:bg-background` where light surfaces were made white.
- Added Playwright coverage:
  - `VA-125`: article body remains viewport-centered while TOC stays separated.
  - `VA-126`: light-mode body, page shell, reading body, content, TOC, related shell, and related cards are white.
- Replaced existing `safe-area` arbitrary Tailwind classes with named CSS utilities.
- Restricted Tailwind v4 source detection to the `src` tree using `@import "tailwindcss" source("../");` so generated reports/backups are not scanned as class sources.
- Recorded task progress in `todolist-2026-05-16.md`.

## Intentionally Not Changed

- No backend API, database, domain, or persistence code was changed.
- No public content pagination behavior was changed.
- No dark-mode palette redesign was performed beyond preserving equivalent dark surfaces for changed components.
- No full visual redesign, typography system change, or card radius system change was performed.
- No remote promotion to `dev` or `main` was executed in this audit step.

## Goal Verification

- Requirement: "Light mode basic color is bad."
  - Result: the root light `--background` now resolves to white, and detail-related surfaces use explicit white classes.
- Requirement: "Inside the border should be white in light mode."
  - Result: detail page shell, reading body, content root, TOC, related shell, and related cards are covered by `VA-126`.
- Requirement: "The center Work/Study content should remain white."
  - Result: existing body/content white behavior remains and is still covered by `VA-123` plus `VA-126`.
- Requirement: "The wider On This Page rail must not push Work/Study content left."
  - Result: `VA-125` verifies both Blog and Work article centers at the 1440px viewport midpoint within 4px.
- Requirement: "The article body and On This Page should feel separate."
  - Result: the body column is centered in a symmetric grid while the TOC is a separate right rail with a minimum 24px separation and readable width.

## Validation Performed

- Red test before implementation:
  - `VA-125|VA-126` failed before implementation.
  - `VA-125` measured the article body center 152px off at 1440px.
  - `VA-126` measured light body background 7 RGB levels below white.
- Focused local Playwright:
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13016 BACKEND_PUBLISH_PORT=18082 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test tests/ui-quality-blog-detail-visuals.spec.ts --grep "VA-125|VA-126" --workers=1`
  - Result: 2 passed.
- Relevant local Playwright regression subset:
  - `tests/ui-quality-blog-detail-visuals.spec.ts`
  - `tests/public-blog-toc-layout.spec.ts`
  - `tests/ui-quality-visual-advanced.spec.ts`
  - `tests/ui-improvement-related-content-width.spec.ts`
  - `tests/ui-improvement-blog-toc.spec.ts`
  - `tests/public-detail-toc-fallback.spec.ts`
  - `tests/mobile-public-navigation.spec.ts`
  - `tests/ui-header-responsive.spec.ts`
  - Result: 25 passed.
- Focused Vitest:
  - `npm test -- src/test/table-of-contents.test.tsx src/test/navbar-mobile-nav.test.tsx`
  - Result: 2 files passed, 14 tests passed.
- Lint:
  - `npm run lint -- 'src/app/(public)/blog/[slug]/page.tsx' 'src/app/(public)/works/[slug]/page.tsx' 'src/app/(public)/layout.tsx' src/components/content/TableOfContents.tsx src/components/content/RelatedContentList.tsx src/components/content/PublicDetailAdjacentLink.tsx src/components/layout/Navbar.tsx tests/ui-quality-blog-detail-visuals.spec.ts`
  - Result: passed.
- TypeScript:
  - `npm run typecheck`
  - Result: passed.
- Production build:
  - `npm run build`
  - Result: passed.
- Docker dev stack:
  - `POSTGRES_DATA_DIR=/home/kimwoonggon/.woong-blog-docker/dev-detail-layout-20260516/postgres BACKEND_PUBLISH_PORT=18180 NGINX_HTTP_PORT=13017 NGINX_HTTPS_PORT=13018 ./scripts/dev-up.sh`
  - Result: frontend and backend images built; frontend Docker `npm run build` passed.
  - Initial nginx start with default `local-https.conf` failed because local certs were absent.
  - Recreated nginx with `NGINX_DEFAULT_CONF=./nginx/default.conf`.
  - Readiness: backend `http://127.0.0.1:18180/api/health` returned ok; frontend `http://127.0.0.1:13017/login` returned 200.
- Focused Docker Playwright:
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13017 BACKEND_PUBLISH_PORT=18180 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test tests/ui-quality-blog-detail-visuals.spec.ts --grep "VA-125|VA-126" --workers=1`
  - Result: 2 passed.
- Whitespace check:
  - `git diff --check`
  - Result: passed.

## Risks And Yellow Flags

- Docker Desktop resource limits were not changed or proven to be 2 core / 8GB in this follow-up.
- Full `npm test` was not run; the targeted Vitest subset passed, and a pre-existing unrelated `public-static-routes` stale mock issue was previously known.
- Full core E2E was not rerun; targeted Docker and local Playwright coverage was used because this change is scoped to public detail layout, surface color, navbar safe-area utilities, and Tailwind source scanning.
- The Docker stack is running on alternate local ports for evidence: frontend `http://127.0.0.1:13017`, backend `http://127.0.0.1:18180`.

## External Reference

- Tailwind CSS source detection documentation: https://tailwindcss.com/docs/detecting-classes-in-source-files

## Final Recommendation

The UI change is locally and Docker validated for the stated goal. The next engineering step is to commit this branch and open/push it toward `dev`; run CI and a full promotion suite before merging to `main`.
