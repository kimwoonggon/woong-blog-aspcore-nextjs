# On This Page TOC Width Audit - 2026-05-11

## Goal
Fix the public detail `On This Page` / `On This Work` table-of-contents rail so the collapse control and TOC text area are not squeezed on desktop layouts.

## Changed
- Added Playwright regression coverage for public Blog and Work detail TOC rails.
- The tests now require the desktop TOC rail to be at least 312px wide, the title area beside the Collapse button to retain at least 220px, and long heading links to retain at least 280px of readable width.
- Changed Blog and Work detail desktop grids from a flexible right `1fr` rail to an explicit `22rem` rail.
- Removed extra `xl:pl-10` padding from the TOC aside so the rail width is used by the TOC, not consumed by inner padding.
- Hardened `TableOfContents` header layout with `min-w-0`, `flex-1`, `truncate`, `shrink-0`, `whitespace-nowrap`, and a stable `4.75rem` toggle button minimum width so the title and Collapse/Expand button do not steal unexpected width from each other.

## Intentionally Not Changed
- No mobile behavior change; the detail TOC remains hidden below `xl` as before.
- No backend/API/DTO/load-test/HLS code was changed.
- No production SSH, remote server operation, or deployment action was performed.
- No cache behavior was changed.

## Goal Verification
- RED reproduced the issue: Blog and Work TOC width measured 144px, below the new 280px minimum.
- RED reproduced the additional header squeeze: after the first width fix, the title area beside Collapse measured 209px, below the new 220px minimum.
- GREEN confirmed the 22rem rail and stable toggle button fix with the same focused Playwright tests.
- The central article body remains capped at `48rem`; only the desktop rail allocation changed.

## Validations
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3010 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npx playwright test tests/public-blog-toc-layout.spec.ts tests/public-work-toc.spec.ts --project=chromium-public --workers=1`
  - RED: failed as expected before implementation, TOC width 144px.
  - RED follow-up: failed as expected before the header/22rem adjustment, title width 209px.
  - GREEN: passed 2 tests after 22rem rail and stable Collapse/Expand button implementation.
- `npx vitest run src/test/table-of-contents.test.tsx --pool=threads --maxWorkers=2`: passed 8 tests.
- `git diff --check`: passed.
- `npm run typecheck`: passed.
- `npm run lint`: passed with 0 errors and 5 existing warnings.
- Docker dev stack was rebuilt and run at `http://127.0.0.1:3010` with backend published on `127.0.0.1:18086`.

## Risks And Yellow Flags
- Wider TOC rail intentionally shifts some desktop horizontal allocation to the right rail. The body width cap remains unchanged, but the whole layout is less symmetrical at the `xl` breakpoint.
- Existing full E2E was not rerun for this UI-only slice; focused E2E and component/unit/type/lint checks were run.
- Existing lint warnings remain unrelated to this change.

## Recommendation
Merge this slice to `dev` after CI. If desired, run the full core E2E suite after merge because this is a layout-level public detail change.
