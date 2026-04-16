# Renovation Plan TODO 0416

Execution ledger for the 2026-04-16 portfolio/admin renovation work. Mark each item `[x]` only after a focused test or explicit verification has passed.

## Public UI

- [x] Remove redundant hero/nav copy around the owner name.
- [x] Replace public-facing `Blog` labels with `Study`.
- [x] Hide public blog/work detail excerpt callouts without deleting excerpt data or SEO metadata.
- [x] Hide excerpts in related cards while preserving the component data path.
- [x] Keep the current item in related lists and highlight it in natural order.
- [x] Default related list page to the page containing the current item when `relatedPage` is absent.
- [x] Put `Previous` on the left and `Next` on the right in blog/work detail navigation.
- [x] Move `On This Page` slightly farther right on desktop detail layouts.

## Admin And Content Persistence

- [x] Fix admin home editor readback for headline, intro text, and profile image.
- [x] Verify home page saves still update the public homepage.
- [x] Verify resume PDF upload links the asset and public resume page updates.
- [x] Keep admin works thumbnails aligned with public thumbnail resolution.

## Navigation State

- [x] Preserve admin blog page/query/pageSize after edit and delete.
- [x] Preserve admin work page/query/pageSize after edit and delete.
- [x] Preserve Blog Notion library scroll position after close/reopen.
- [x] Keep the current Notion document visible after reopening the library.

## Inline Public Admin UX

- [x] Show public inline edit/delete controls near the detail header.
- [x] Add delete actions for public inline blog/work admin views.
- [x] Close public work inline editor cleanly after save.

## Theme

- [x] Keep light mode as the default regardless of system theme.
- [x] Keep only Light/Dark choices and hide System.
- [x] Soften dark-mode tokens to reduce eye strain.
- [x] Verify dark/light focus and browser chrome behavior.

## AI Batch Jobs

- [x] Remove continuous AI batch job polling.
- [x] Remove idle backend AI batch queue/completed-job polling; backend now wakes on job creation signals.
- [x] Keep explicit `Refresh jobs` behavior.
- [x] Update tests so no-polling is the expected behavior.

## Runtime

- [x] Keep compose/nginx as the supported full-stack verification path.
- [x] Allow dev HTTPS nginx to run on a non-privileged host port.
- [x] Keep main/prod compose production-shaped and free of dev-only affordances.
- [x] Bind dev/main/staging/security published ports to `127.0.0.1` by default, with explicit env override for intentional external exposure.

## Verification

- [x] Focused Vitest for changed components/hooks.
- [x] Focused backend tests for admin page readback.
- [x] Focused Playwright for admin pages, pagination, Notion library, public details, theme, and upload flows.
- [x] Lint, typecheck, build.
