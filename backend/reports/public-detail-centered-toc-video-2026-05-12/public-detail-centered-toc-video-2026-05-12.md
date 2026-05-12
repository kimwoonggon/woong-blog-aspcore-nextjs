# Public Detail Centering, Home Works Count, Video Upload E2E Audit - 2026-05-12

## Summary

This change fixes the dev-scoped UI and E2E issues raised for public detail pages, home Works count, and video upload verification.

- Public Blog and Work detail bodies now remain centered when the desktop TOC rail is present.
- The TOC rail only appears at `1600px` and wider, where there is enough horizontal room for centered content plus the rail.
- `TableOfContents` spacing was tightened so the collapse button and visible item text have more usable width.
- Home featured Works now returns up to 4 items instead of 3, avoiding an odd empty-looking mobile grid.
- Existing E2E checks were updated to match the new `1600px` TOC rail breakpoint.
- Full dev E2E and focused video upload/HLS checks were run against local dev compose.

## Changed Files

- `src/app/(public)/blog/[slug]/page.tsx`
- `src/app/(public)/works/[slug]/page.tsx`
- `src/components/content/TableOfContents.tsx`
- `backend/src/WoongBlog.Infrastructure/Modules/Composition/Persistence/HomeQueryStore.cs`
- `backend/tests/WoongBlog.Api.ComponentTests/PublicQueryHandlerComponentTests.cs`
- `tests/public-blog-toc-active.spec.ts`
- `tests/public-blog-toc-layout.spec.ts`
- `tests/public-detail-toc-fallback.spec.ts`
- `tests/public-work-toc.spec.ts`
- `tests/ui-improvement-blog-toc.spec.ts`
- `tests/ui-improvement-featured-works-grid.spec.ts`
- `tests/ui-improvement-scroll-margin.spec.ts`
- `tests/ui-loading-states.spec.ts`
- `tests/ui-quality-blog-detail-visuals.spec.ts`
- `tests/ui-quality-visual-advanced.spec.ts`
- `tests/admin-work-publish.spec.ts`
- `todolist-2026-05-12.md`

## Intentionally Not Changed

- No production SSH, production deploy, server pull, or production `.env` work was performed.
- No cache shortcut was added.
- No seed URL shortcut was added for Real Backend Test.
- Public list `pageSize=12` behavior was not reduced.
- Public DTO shapes were not broadened to include hidden body/media data for list pages.
- Article body max width remains constrained; the layout change centers it rather than widening the reading column.

## Goal Verification

- Body centering: verified with Playwright assertions for Blog and Work detail content center alignment.
- TOC readability: rail breakpoint and TOC spacing were updated; TOC-related specs passed at the new desktop breakpoint.
- Mobile home Works count: backend query returns up to 4 featured Works; Playwright creates four published fixtures and verifies the mobile home section shows four cards.
- Video upload: admin/public video upload flows and S3-compatible HLS processing checks passed.
- Dev-only scope: all runtime validation used local dev compose; no remote production work was performed.

## Validations

- RED focused backend component test failed before changing `Take(3)` to `Take(4)`.
- GREEN focused backend component test passed after the Home query update.
- `npx eslint` focused checks passed for changed UI/test files.
- `npx vitest --run src/test/table-of-contents.test.tsx` passed, 9 tests.
- `npm run build` passed.
- `dotnet test backend/WoongBlog.sln` passed.
- Focused Playwright layout checks passed, 4 tests.
- Focused mobile home Works check passed, 1 test.
- Focused video upload checks passed, 7 tests.
- S3-compatible HLS processing check passed, 1 test.
- Additional focused E2E regression checks passed, 12 tests.
- Final focused flake-hardening checks passed, 5 tests.
- Full dev E2E passed: 431 passed, 4 skipped, latency budget failures 0.
- `git diff --check` passed.

## Risks And Yellow Flags

- The TOC rail is no longer shown at 1440px. This is intentional to keep the article body visually centered; the rail appears from 1600px where the page has enough width.
- Full E2E completed against the current local dev compose database after prior test setup/fixtures, not a freshly dropped database volume.
- Playwright reported 14 latency warnings in the full E2E summary, but no latency budget failures.
- `npm ci` reported dependency vulnerability counts during setup; dependency remediation was outside this task.

## Final Recommendation

Use this dev-scoped change as the current candidate for the public detail layout and home Works grid fix. If clean-volume confidence is required before merge, rerun dev compose from an empty database volume and execute `npm run test:e2e` once more.
