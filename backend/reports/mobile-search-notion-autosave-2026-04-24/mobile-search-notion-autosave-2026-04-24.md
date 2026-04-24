# Mobile Search / Notion Autosave Audit

- Date: 2026-04-24
- Scope: mobile public search UX, mobile feed append behavior, blog notion autosave latency, excerpt/body complaint triage

## Changed
- Moved mobile public search opening to the shared top navbar and rendered the mobile search form directly under the sticky header in [Navbar.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/layout/Navbar.tsx).
- Hid the page-level `/blog` and `/works` search form on mobile by making [PublicSearchForm.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/layout/PublicSearchForm.tsx) desktop-only.
- Removed auto-append on compact feeds from [PublicResponsiveFeed.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/content/PublicResponsiveFeed.tsx); mobile/tablet now append only on explicit `Load more`.
- Reduced notion autosave debounce and surfaced `Saving...` immediately in [BlogNotionWorkspace.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/admin/BlogNotionWorkspace.tsx).
- Added a short authenticated-session TTL cache in [auth.ts](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/lib/api/auth.ts) so repeated autosave mutations do not preflight `/auth/session` every time.
- Added/updated behavior coverage in:
  - [navbar-mobile-nav.test.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/test/navbar-mobile-nav.test.tsx)
  - [public-responsive-feed.test.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/test/public-responsive-feed.test.tsx)
  - [blog-notion-workspace.test.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/test/blog-notion-workspace.test.tsx)
  - [auth-csrf.test.ts](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/test/auth-csrf.test.ts)
  - [mobile-public-navigation.spec.ts](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/mobile-public-navigation.spec.ts)

## Not Changed
- The excerpt/body split on content cards was not globally rewritten. Investigation showed the current design intentionally uses `excerpt` on preview/card surfaces and full `contentJson`/admin detail payloads on detail or edit surfaces.
- Desktop pagination behavior for `/blog` and `/works` remains unchanged.
- Notion autosave still uses the existing full blog update endpoint; this pass reduced latency without introducing a content-only API.

## Goal Check
- Mobile search now has one primary opener in the navbar: yes.
- Mobile search opens without dropping the reader back to the top: yes, verified in Playwright.
- Page-body mobile search chrome is hidden by default: yes.
- Mobile feed no longer auto-loads while scrolling: yes.
- Notion autosave feels faster and shows pending state earlier: yes.
- Excerpt/body complaint root cause clarified: yes.

## Validation
- `npx vitest run src/test/navbar-mobile-nav.test.tsx src/test/public-responsive-feed.test.tsx src/test/blog-notion-workspace.test.tsx src/test/auth-csrf.test.ts`
  - Result: pass, 23 tests
- `npm run typecheck`
  - Result: pass
- `npm run lint`
  - Result: pass with pre-existing warnings only
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/mobile-public-navigation.spec.ts tests/public-blog-pagination.spec.ts tests/public-works-pagination.spec.ts tests/ui-admin-notion-autosave-info.spec.ts --workers=1`
  - Result: pass, 13 tests
- Docker HTTP stack re-validated on `http://127.0.0.1:3000`
  - Result: responding after rebuild/restart

## Risks / Follow-up
- The authenticated-session TTL cache intentionally trades immediate preflight freshness for fewer autosave round-trips. Expired sessions still redirect on mutation response, but the redirect may happen one request later than a strict preflight model.
- The excerpt/body complaint may still merit a product decision later if preview surfaces should use first-paragraph body text instead of the current explicit excerpt field.
- The Playwright default internal web-server path still assumes a backend on `localhost:8080`; external-server mode was required for this pass because the validated Docker HTTP stack runs through `127.0.0.1:3000` with backend published on `18080`.

## Recommendation
- Keep this pass as-is.
- If the next priority is notion editing speed beyond this improvement, split content autosave onto a content-only backend endpoint and remove duplicate content parsing on the server path.
