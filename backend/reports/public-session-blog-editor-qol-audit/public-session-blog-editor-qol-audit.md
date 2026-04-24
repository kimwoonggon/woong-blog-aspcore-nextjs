# Public Session + Blog Editor QoL Audit

Date: 2026-04-21

## Summary

This PR stays focused on public/admin session rendering consistency, contact page authored-content fidelity, and BlogEditor workflow quality-of-life improvements.

## Changed

- Removed the redundant public layout session fetch; `Navbar` no longer receives or declares an unused session prop.
- Added `src/lib/auth/public-admin.ts` as the shared public admin-affordance gate.
- Updated public blog list/detail, works list/detail, contact, introduction, and resume pages to use the shared public admin-affordance state.
- Changed `PublicAdminLink` into a pure render component that receives `canShow` from the page-level gate instead of fetching session on its own.
- Removed contact page `hasMailto`/fallback direct-email injection and the `__qaNoMailto` override path so authored content is rendered as-is.
- Added a top BlogEditor action bar with quick save and back controls while retaining the existing bottom Create/Update submit button.
- Added a horizontally resizable BlogEditor workspace wrapper.
- Kept excerpt as a first-class editable field and covered excerpt-aware create/update/inline update flows.
- Changed the browser default API base to same-origin `/api` so admin browser mutations work under the Docker dev stack CSP instead of resolving to `http://localhost/api`.
- Updated focused Vitest and Playwright coverage for public admin gates, contact fallback removal, BlogEditor excerpt/top-save behavior, same-origin API defaults, and stale-session editor behavior.

## Intentionally Not Changed

- Backend auth/security semantics, role checks, session endpoint shape, CSRF behavior, admin authorization, and endpoint routes were not changed.
- Public read pages remain accessible to anonymous users.
- Blog cards and related cards still use excerpt-first presentation; rendered content snippets were not introduced.
- Mobile infinite scroll, feed prefetch redesign, mobile search redesign, broad works/study density tuning, related-content restructuring, and autosave debounce redesign were deferred.
- The `DOMMatrix is not defined` resume/PDF SSR issue was diagnosed but not fixed in this PR because it is outside the requested scope.

## Goal Check

- Public/admin session cleanup: met. Public layout no longer fetches session, and public admin affordance visibility flows through one helper.
- Contact authored-content fidelity: met. The contact page no longer injects a fallback direct-email block when authored content lacks `mailto:`.
- BlogEditor QoL: met. Top quick save, top back, bottom submit retention, horizontal workspace resize, and excerpt-preserving update paths are covered.
- Presentation guardrails: met. Feed and related cards remain excerpt-based.

## Validations

- `npx skills find nextjs testing blog editor admin session` -> no stronger high-confidence skill found; used local `tdd`, Next.js, and React guidance.
- `npx vitest run src/test/api-base.test.ts src/test/auth-csrf.test.ts src/test/auth-login-url.test.ts src/test/page-editor.test.tsx src/test/blog-editor.test.tsx src/test/public-admin-rendering.test.tsx --pool=threads` -> 6 files passed, 38 tests passed.
- `npm run lint` -> passed with 5 existing warnings.
- `npm run typecheck` -> passed.
- `npm test -- --run` -> 47 files passed, 247 tests passed.
- `BACKEND_PUBLISH_PORT=18080 NGINX_DEFAULT_CONF=./nginx/default.conf ./scripts/dev-up.sh` -> Docker dev stack rebuilt and started; Next production build passed.
- `curl -fsS http://127.0.0.1:3000/api/auth/session` -> `{"authenticated":false}`.
- Focused Playwright subset against Docker dev stack -> 14 passed, 1 existing skipped:
  - `tests/public-contact-fallback-email.spec.ts`
  - `tests/public-admin-affordances.spec.ts`
  - `tests/public-inline-editors.spec.ts`
  - `tests/public-blog-detail-inline-edit.spec.ts`
  - `tests/public-blog-inline-redirects.spec.ts`
  - `tests/ui-admin-blog-excerpt.spec.ts`
- Full Playwright e2e with 4 workers -> 552 passed, 10 failed, 9 skipped.
- Serial rerun of the 10 failed full-e2e lines with 1 worker -> 29 passed.
- Grep checks:
  - Public route session/role checks are centralized in `src/lib/auth/public-admin.ts`.
  - Contact fallback strings are absent from production source.

## Risks And Yellow Flags

- Full e2e is not clean under 4 workers. Every failed line passed when rerun serially, so the failures point to parallel shared-state/test isolation issues rather than this PR's product behavior.
- Frontend logs show repeated `ReferenceError: DOMMatrix is not defined` when `/resume` loads. Diagnosis: `react-pdf`/`pdfjs-dist` is being evaluated in the Next.js SSR bundle through `ResumePdfViewer`; fixing it should be a separate small PR by moving the PDF renderer behind a client-only dynamic import.
- Changing the browser default API base to `/api` is intentional for same-origin CSP compatibility. Deployments that require an absolute API origin can still use `NEXT_PUBLIC_API_BASE_URL`.
- Contact pages without authored email links will no longer receive a synthetic direct-email fallback. This is the requested authored-content behavior.

## Recommendation

Proceed with this PR as a focused frontend/admin consistency and editor ergonomics change. Follow up separately on the resume PDF `DOMMatrix` SSR issue and on making the full e2e suite safe for 4-worker execution.
