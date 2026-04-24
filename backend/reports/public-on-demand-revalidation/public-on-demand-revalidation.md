# Public On-Demand Revalidation Audit

Date: 2026-04-22

## Summary

This change adds protected, on-demand public cache invalidation after successful admin mutations. Public content fetches now carry stable cache tags, and admin create/update/delete flows call a small Next route at `/revalidate-public` after the backend mutation has succeeded.

## Changed

- Added a protected Next route handler for public revalidation.
- Added shared helpers for normalizing public paths and mapping paths to public data cache tags.
- Added a small client helper that calls the revalidation route after successful admin mutations.
- Wired revalidation into blog, work, page, home, resume, and site settings mutation flows.
- Updated public fetch helpers to tag public data caches.
- Updated focused unit tests and Playwright specs so tests wait for `/revalidate-public` before asserting refreshed public content.
- Removed one unused `WorkEditor` direct-upload helper that had no call site.

## Intentionally Not Changed

- Backend API contracts and auth semantics were not changed.
- Public anonymous read access was not restricted.
- Editor UI flows were not redesigned.
- Mobile/feed/search/autosave work was not included.
- Global Playwright 4-worker isolation was not addressed; validation used one worker as requested.

## Invalidation Map

- Blog create/update/delete: `/`, `/blog`, `/blog/<nextSlug>`, and `/blog/<previousSlug>` when applicable.
- Work create/update/delete: `/`, `/works`, `/works/<nextSlug>`, and `/works/<previousSlug>` when applicable.
- Contact page save: `/contact`.
- Introduction page save: `/introduction`.
- Home page save: `/`.
- Resume upload/delete: `/resume`.
- Site settings save: `/`, `/blog`, `/works`, `/contact`, `/introduction`, and `/resume`.

The route also invalidates matching public fetch tags such as `public-home`, `public-site-settings`, `public-blogs`, `public-works`, detail tags, and page/resume tags. This is necessary because public route output and external public API fetch caches can be cached separately.

## Validation

- `npx vitest run src/test/public-revalidation-paths.test.ts src/test/public-revalidation-route.test.ts src/test/blog-editor.test.tsx src/test/work-editor.test.tsx src/test/page-editor.test.tsx src/test/resume-editor.test.tsx src/test/inline-blog-editor-section.test.tsx src/test/inline-work-editor-section.test.tsx src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts --pool=threads` -> 10 files passed, 72 tests passed.
- `npm run typecheck` -> passed.
- `npm run lint` -> blocked by local Docker-owned `.tmp/postgres` permission during root scan.
- `npx eslint src tests playwright.config.ts next.config.ts eslint.config.mjs` -> passed with 3 existing warnings.
- Docker/nginx frontend rebuild -> passed; Next production build succeeded.
- Focused Playwright revalidation group -> 16 passed.
- First full e2e single-worker after implementation -> 562 passed, 1 failed, 8 skipped.
- Targeted rerun after fixing the test race -> `tests/public-inline-editors-unsaved-warning.spec.ts` 2 passed.
- Final full e2e single-worker -> 563 passed, 8 skipped.
- `npm test -- --run` -> 51 files passed, 259 tests passed.

## Risks And Yellow Flags

- `npm run lint` is still sensitive to the local Docker `.tmp/postgres` directory because ESLint scans it before ignore handling can avoid the permission error. Source/test scoped ESLint passed.
- The revalidation endpoint intentionally depends on the existing public admin affordance/session gate. If that helper changes, this route should be re-reviewed.
- `revalidateTag(tag, 'max')` marks data stale for Next's cache profile. In this app, the client waits for the route response before reading public pages; browser validation passed.

## Recommendation

Proceed with this focused change. A later infrastructure cleanup should make root `npm run lint` ignore local Docker `.tmp` state without requiring scoped lint commands.
