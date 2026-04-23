# Broad Frontend Refactor Audit - 2026-04-22

## Summary

Implemented a broad frontend refactor focused on public runtime boundaries, pagination stability, admin authoring latency, public reading polish, and SEO basics.

Changed:
- Kept `/resume` on the existing client-only PDF boundary and verified it in browser flows.
- Moved public blog/work detail admin edit payload loading out of server render and behind client admin gates.
- Added lazy public detail admin action components for blog/work edit/delete, loading admin payloads only when the editor is opened.
- Deduplicated client-side public admin session checks across multiple public admin gates.
- Preserved explicit tiny `pageSize=2` perf/deep-link scenarios while normalizing unsupported public list page sizes.
- Added public metadata helpers, route metadata for key public pages, and `robots.txt`/`sitemap.xml`.
- Improved related cards by keeping excerpts lightweight for non-current related items, reducing tag clutter, and preventing related-card text from polluting form accessible names.
- Tightened blog editor dirty-state handling so Tiptap initial normalization does not enable save, while real edits still do.
- Removed remaining public route `headers()` usage from home/introduction QA paths.

Intentionally not changed:
- Backend auth, role checks, CSRF, sessions, and API security semantics.
- Browser API same-origin `/api` defaults.
- Excerpt-first list/card strategy.
- Existing revalidation endpoint and mutation invalidation map except verification through tests.
- Broad infinite scroll, autosave redesigns, backend architecture, or folder-only reshuffles.

## Goal Check

- Anonymous public reading: preserved and covered by public Playwright flows.
- Admin public affordances: preserved through `PublicAdminClientGate`; detail edit payloads now load lazily after gate approval.
- Cache/rendering boundaries: public detail routes no longer force dynamic rendering for server-side admin checks; build output still marks public routes dynamic in this environment because the request-aware public API fallback remains.
- Immediate public invalidation: existing revalidation flows remain wired and focused tests still pass.
- Pagination: focused Study/Works pagination and edge navigation pass, including page 2 flows.
- `/resume` SSR boundary: focused resume specs pass; no server-side PDF evaluation was reintroduced.
- Reading UX: related cards are lighter, avoid current-article duplicate excerpts, and avoid accessibility-name collisions.
- SEO: route metadata helper, key route metadata, sitemap, and robots were added.

## Validation

- `npx vitest run ... --pool=threads` focused public/admin/revalidation/editor suite: 16 files, 90 tests passed.
- Additional focused Vitest after fixes:
  - `src/test/blog-editor.test.tsx`: 8 passed.
  - `src/test/related-content-list.test.tsx`: 3 passed.
  - `src/test/public-admin-rendering.test.tsx` + `src/test/responsive-page-size-sync.test.tsx`: 9 passed.
  - `src/test/blog-editor.test.tsx src/test/related-content-list.test.tsx src/test/responsive-page-size-sync.test.tsx src/test/seo-metadata.test.ts`: 16 passed.
- `npm run typecheck`: passed.
- `npm run build`: passed locally.
- Docker compose frontend rebuild with `BACKEND_PUBLISH_PORT=18080 NGINX_DEFAULT_CONF=./nginx/default.conf`: passed; stack served on `http://127.0.0.1:3000`.
- `npm run lint`: blocked by existing Docker-owned `.tmp/postgres` permission (`EACCES`).
- Scoped `npx eslint src tests playwright.config.ts next.config.ts eslint.config.mjs`: passed with 3 existing warnings.
- Focused Playwright public/runtime group: after fixes, targeted blog/work inline edit, pagination, resume, SEO, and public admin affordance checks passed.
- Full E2E via `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible npm run test:e2e`: 560 passed, 8 failed, 9 skipped on first full run after implementation.
- Targeted reruns for the full-run failures after fixes:
  - `feature-recording-0418.spec.ts:303`: passed.
  - `public-contact-fallback-email.spec.ts`: passed.
  - `admin-home-image-upload.spec.ts`: passed.
  - `admin-mermaid-public-independence.spec.ts`: passed.
  - `admin-site-settings-extreme-input.spec.ts`: passed.
  - `manual-qa-gap-coverage.spec.ts:450`: passed.
  - `renovation-0416-regression.spec.ts:43`: passed.
  - `public-edge-nav.spec.ts`: 3 passed.
- Origin/dev comparison smoke:
  - Command used one measured iteration and no warmups on isolated ports `32080/32081/18180`.
  - Report paths:
    - `backend/reports/frontend-performance-origin-dev-vs-current/frontend-performance-origin-dev-vs-current.md`
    - `backend/reports/frontend-performance-origin-dev-vs-current/frontend-performance-origin-dev-vs-current.html`
    - `backend/reports/frontend-performance-origin-dev-vs-current/frontend-performance-origin-dev-vs-current.json`

## Risks And Yellow Flags

- The origin/dev benchmark is a one-sample smoke, not a statistically stable performance conclusion.
- Benchmark still reports current regressions in cold Study/Works loads and several public API timings, despite focused Playwright pagination passing. The report also records a current Study Next timeout; treat this as a benchmark-specific follow-up because `tests/public-blog-pagination.spec.ts` passed page 2 navigation.
- Public routes still appear dynamic in Next build output in this compose/build environment. The server-side admin/session coupling was removed, but the request-aware API-base fallback still prevents a clean static classification.
- Full E2E was not rerun from scratch after the final targeted fixes because the first full run took 16.4 minutes; all failed specs were rerun directly and passed.

## Final Recommendation

Proceed with review of this frontend refactor, but do not treat the one-sample benchmark as release-grade performance evidence. The next best follow-up is a dedicated performance pass on public route static classification and Study/Works cold load behavior using multiple benchmark iterations on a clean stack.
