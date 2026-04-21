# Resume DOMMatrix SSR Fix Audit

Date: 2026-04-22

## Summary

This change fixes the `/resume` runtime issue where `react-pdf`/`pdfjs-dist` was being pulled into the server SSR bundle and crashing with `ReferenceError: DOMMatrix is not defined`.

## Changed

- Moved the browser-only PDF rendering implementation into `src/components/content/ResumePdfDocument.tsx`.
- Kept `src/components/content/ResumePdfViewer.tsx` as a lightweight client wrapper that uses `next/dynamic(..., { ssr: false })` to load the actual PDF renderer only on the client.
- Preserved the existing `/resume` page layout and the stable `data-testid="resume-pdf-viewer"` shell used by Playwright.
- Kept the existing loading/error viewer copy, with slightly friendlier “Loading resume preview...” wording.
- Added a focused Vitest guard in `src/test/resume-server-render.test.tsx` to verify the wrapper module can load without importing `react-pdf`.
- Updated focused `/resume` Playwright tests to assert the route returns HTTP 200 before checking page content.

## Intentionally Not Changed

- No backend API or auth behavior was changed.
- No resume page layout redesign or caching strategy work was added.
- No unrelated blog/work/contact/product flows were touched.
- No global Playwright worker isolation cleanup was bundled here.
- The broader full-suite parallel instability investigation is deferred to a separate infrastructure/testing PR.

## Goal Check

- Prevent browser-only PDF code from being evaluated in the server SSR bundle: met.
- Keep `/resume` publicly accessible to anonymous users: met.
- Preserve current UX and states: met.
- Avoid hydration mismatch-prone redesign: met.

## Validations

- `npx vitest run src/test/resume-server-render.test.tsx --pool=threads` -> 1 file passed, 1 test passed.
- `npm run lint` -> passed with 5 existing warnings elsewhere in the repo.
- `npm run typecheck` -> passed.
- `BACKEND_PUBLISH_PORT=18080 NGINX_DEFAULT_CONF=./nginx/default.conf ./scripts/dev-up.sh` -> Docker dev stack rebuilt; Next production build passed.
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/public-resume-empty-state.spec.ts tests/resume.spec.ts --workers=1` -> 3 passed.
- `docker compose -f docker-compose.dev.yml logs frontend | rg "DOMMatrix|ReferenceError"` -> no matches after rebuild.

## Risks And Yellow Flags

- The focused Vitest guard proves the wrapper module no longer imports `react-pdf`, but it does not simulate every nuance of Next.js’ server bundling. The stronger signal is that the rebuilt `/resume` route now loads in Playwright and frontend logs no longer show `DOMMatrix`.
- The inner PDF renderer still uses `react-pdf` and `pdfjs-dist` on the client, so any client-side library regressions remain possible, but they are outside this SSR fix.
- Full-suite Playwright parallel instability remains a separate issue and was not addressed here.

## Recommendation

Proceed with this as a small dedicated `/resume` runtime fix. Follow with a separate PR for Playwright 4-worker isolation and, if needed, additional resume viewer polish.
