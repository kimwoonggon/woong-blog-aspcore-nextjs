# Full E2E Docker Rerun Audit - 2026-05-14

## Scope

- User request: start the Docker dev stack and rerun full E2E thoroughly.
- Environment: local dev Docker stack only, `http://127.0.0.1:3000`, backend published on `127.0.0.1:18080`.
- Explicit non-scope: production SSH/server work.

## Changes Made

- Corrected the Study archive desktop grid to use the stable Tailwind `xl:grid-cols-3` class in `src/components/content/PublicResponsiveFeed.tsx`.
- Updated existing-work automatic video thumbnail flow in `src/components/admin/WorkEditor.tsx` so generated thumbnails are persisted and public Work paths are revalidated before the ready preview is exposed.
- Added/updated guards in `src/test/public-responsive-feed.test.tsx` and `src/test/work-editor.test.tsx` for the grid class and thumbnail revalidation behavior.
- Updated `todolist-2026-05-14.md` with the Docker/E2E execution record.
- Created backups under `.agent-backups/full-e2e-docker-rerun-2026-05-14/` before source/test edits.

## Intentionally Not Changed

- No backend API, database schema, HLS transcoder, or Docker compose configuration changes were made.
- Production/SSH workflows were not touched.
- Existing skipped tests remain skipped.
- The dev Docker stack was left running for follow-up inspection.
- Pre-existing unrelated dirty files in the repository were not reverted or edited.

## Failure Analysis

Initial full E2E result:

- `427 passed`, `4 failed`, `4 skipped`.
- Failed tests:
  - `tests/public-layout-stability.spec.ts` desktop Study grid row alignment.
  - `tests/ui-improvement-blog-card-anchors.spec.ts` Study grid 3-column expectation.
  - `tests/ui-improvement-blog-stripe-grid.spec.ts` Study grid 3-column expectation.
  - `tests/work-inline-create-flow.spec.ts` existing work uploaded-video thumbnail visibility on public `/works`.

Root causes confirmed by artifacts and runtime inspection:

- Study archive rendered two columns where the E2E contract requires three columns at xl desktop. The local file had `min-[1200px]:grid-cols-3`; it was corrected to `xl:grid-cols-3`.
- For the existing-work thumbnail failure, DB and `/api/public/works` already contained the generated `/media/work-thumbnails/...jpg` URL, but the rendered `/works` page still showed `No Image`.
- Nginx access logs showed `POST /revalidate-public` returning `499` during the failing flow, meaning the browser navigated away after the preview appeared and aborted the revalidation request.
- The fix delays exposing the ready preview for existing-work generated thumbnails until the thumbnail PUT and public path revalidation have completed.

## Validation

- Docker dev stack startup:
  - `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` built backend/frontend.
  - Initial nginx start hit a stale Docker Desktop/WSL bind-mount error for `/etc/nginx/nginx.conf`; the nginx container was force-recreated and started successfully.
- Readiness:
  - `npm run test:e2e:readiness` passed for Docker daemon, backend health, and frontend.
  - `/`, `/login`, and `/api/health` returned 200.
- Targeted unit tests:
  - `npm test -- --run src/test/public-responsive-feed.test.tsx src/test/work-editor.test.tsx`
  - Result: `2 passed`, `57 passed`.
- Docker frontend rebuild:
  - `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d --no-deps --build frontend`
  - Next build and TypeScript completed successfully.
- Focused E2E rerun:
  - `tests/public-layout-stability.spec.ts`, `tests/ui-improvement-blog-card-anchors.spec.ts`, `tests/ui-improvement-blog-stripe-grid.spec.ts`, `tests/work-inline-create-flow.spec.ts`
  - Result: `12 passed`, `0 failed`.
- Full E2E rerun:
  - `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npm run test:e2e`
  - Result: `431 passed`, `4 skipped`, `0 failed` in `13.0m`.
  - Latency summary: `testCount=435`, `budgetFailureCount=0`, `warningCount=67`.
- Targeted ESLint:
  - `npx eslint src/components/content/PublicResponsiveFeed.tsx src/components/admin/WorkEditor.tsx src/test/public-responsive-feed.test.tsx src/test/work-editor.test.tsx`
  - Result: `0 errors`, `2 warnings` from existing test mocks using `<img>`.
- Whitespace check:
  - `git diff --check -- ...` passed.
- Docker status after validation:
  - `backend`, `db`, `frontend`, and `nginx` containers are up.

## Risks And Follow-Ups

- Full E2E creates dev DB/media artifacts; clean dev data only if a fresh fixture state is needed.
- Targeted ESLint still reports two warnings from test-only `next/image` mocks.
- The repository already had many unrelated dirty files before this task; final review should isolate this task's source/test/report/TODO changes from unrelated work.
- One Docker rebuild observed a large context while test artifacts existed; the final rebuild context was small, but Docker context hygiene should remain a separate check if it regresses.

## Recommendation

Keep the changes. The Docker dev stack is currently green for the full E2E suite, including upload/HLS, public grid layout, admin flows, and runtime-auth flows.
