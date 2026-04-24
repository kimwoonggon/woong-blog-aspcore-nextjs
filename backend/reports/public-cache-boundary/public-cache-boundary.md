# Public Cache Boundary Audit

Date: 2026-04-22

## Summary

This focused change makes public read fetches more cache/revalidate friendly and decouples static public page content from server-side admin affordance session checks.

## Changed

- Added `src/lib/api/public-cache.ts` with a shared `PUBLIC_CONTENT_REVALIDATE_SECONDS = 60`.
- Added `src/lib/api/public-server.ts` so public read helpers can avoid importing `next/headers`/cookies when an absolute public/internal API origin is available.
- Updated public content helpers to use `next: { revalidate: 60 }` instead of blanket `cache: 'no-store'`:
  - `fetchPublicHome`
  - `fetchPublicBlogs`
  - `fetchPublicBlogBySlug`
  - `fetchPublicWorks`
  - `fetchPublicWorkBySlug`
  - `fetchPublicPageBySlug`
  - `fetchPublicSiteSettings`
  - `fetchResume`
- Added `PublicAdminClientGate` so contact, introduction, and resume can render public content without a server-side session fetch.
- Updated contact/introduction/resume to use `revalidate = 60` and client-side admin affordance gating.
- Updated the home route to use `revalidate = 60` and only touch request headers for local QA flags.
- Updated focused tests for public fetch options and admin gate behavior.

## Routes More Cache-Friendly

- `/`
  - Uses revalidated public home data.
  - Only reads request headers for local QA query flags.
- `/contact`
  - Public page content fetch is revalidated.
  - Admin inline editor gate is client-side.
- `/introduction`
  - Public page content fetch is revalidated.
  - Admin inline editor gate is client-side.
  - Local QA broken-page hook still requires request headers when explicitly used.
- `/resume`
  - Public resume fetch is revalidated.
  - Admin upload affordance gate is client-side.
- Public API helper calls for blog/work list/detail now use revalidated public data fetches, even though their routes remain dynamic for other reasons.

## Intentionally Still Dynamic

- `/blog`
  - Still has search/pagination query handling, local QA hooks, and server-side admin create affordance.
- `/blog/[slug]`
  - Still performs server-side admin detail payload loading for inline editing when an admin session exists.
- `/works`
  - Still has search/pagination query handling, local QA hooks, and server-side admin create affordance.
- `/works/[slug]`
  - Still performs server-side admin detail payload loading for inline editing when an admin session exists.
- Build output currently still marks public routes dynamic when no absolute API origin is available at build time; this avoids Docker image builds depending on a live backend.

## Validations

- `npx vitest run src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts src/test/public-admin-rendering.test.tsx src/test/public-admin-client-gate.test.tsx --pool=threads` -> 4 files passed, 24 tests passed.
- `npm run typecheck` -> passed.
- `npm run lint` -> passed with 5 existing warnings.
- `npm run build` -> passed.
- Focused Playwright against local dev server was attempted, but the existing Docker/nginx API target at `127.0.0.1:3000` was unavailable, causing auth/API proxy `ECONNREFUSED` before assertions could run.

## Risks And Yellow Flags

- This improves fetch cacheability and removes server session coupling from static public pages, but it does not make every public route statically prerendered in the current Docker build environment.
- `getPublicServerApiBaseUrl()` falls back to request-aware `getServerApiBaseUrl()` when no absolute API origin is configured. That preserves local/dev behavior but keeps those builds dynamic.
- Client-side admin affordance gates mean admin edit/upload controls appear after hydration instead of being present in the initial HTML.
- Blog and works routes still need a separate pass if server-side admin affordances are to be fully decoupled.

## Recommendation

Proceed with this as a focused caching-boundary improvement. A later pass can move blog/works admin affordances to client-side gates or route handlers if full static/revalidated public route output is required in the default Docker build.
