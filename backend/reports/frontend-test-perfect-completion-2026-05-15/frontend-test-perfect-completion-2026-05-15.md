# Frontend Test Perfect Completion - 2026-05-15

## Summary

This follow-up completes the gap identified in the earlier frontend test validity audit.

The stale Vitest failure in `src/test/public-static-routes.test.ts` was fixed by aligning the test with the current `generateStaticParams` implementation. The current implementation uses paginated `fetchPublicBlogs(page, 100)` and `fetchPublicWorks(page, 100)`, not the older `fetchAllPublicBlogs` / `fetchAllPublicWorks` helpers.

After the test fix, the full Vitest suite is green. The Docker-backed core Playwright E2E suite also passed under an explicit compose resource profile whose service limits total 2.0 vCPU and 8GiB memory.

## Changed

- Updated `src/test/public-static-routes.test.ts`.
- Added `backend/reports/frontend-test-perfect-completion-2026-05-15/docker-compose.e2e-2core-8gb.override.yml`.
- Updated `todolist-2026-05-15.md` with the hardening follow-up checklist and verification evidence.
- Added this follow-up audit report.

## Intentionally Not Changed

- No production frontend code was changed.
- No backend code was changed.
- No Playwright specs were changed.
- No Docker base compose file was changed.
- No production, CI, SSH, GHCR, or deployment configuration was changed.

## Static Route Test Fix

The fixed test now verifies current behavior:

- Blog and work static params are built from all paginated public list pages.
- Malformed slugs are filtered.
- First-page fetch failure returns an empty params list.
- Later-page fetch failure keeps the params collected from earlier pages.
- The static params helpers call `fetchPublicBlogs(1, 100)`, `fetchPublicBlogs(2, 100)`, `fetchPublicWorks(1, 100)`, and `fetchPublicWorks(2, 100)` where applicable.

## 2core/8GB Docker Profile

The override file applies these runtime limits:

| Service | CPU | Memory |
| --- | ---: | ---: |
| frontend | 0.60 | 3GiB |
| backend | 0.80 | 3GiB |
| db | 0.40 | 1.5GiB |
| nginx | 0.20 | 512MiB |
| Total | 2.00 | 8GiB |

`docker inspect` confirmed the effective limits:

| Container | NanoCpus | Memory bytes | MemorySwap bytes |
| --- | ---: | ---: | ---: |
| frontend | 600000000 | 3221225472 | 3221225472 |
| backend | 800000000 | 3221225472 | 3221225472 |
| db | 400000000 | 1610612736 | 1610612736 |
| nginx | 200000000 | 536870912 | 536870912 |

## Validation

| Command | Result |
| --- | --- |
| `npx vitest run src/test/public-static-routes.test.ts --pool=threads --maxWorkers=2 --reporter=dot` | Passed: 1 file, 7 tests. |
| `npm test` | Passed: 90 files, 627 tests, 1268.87s. |
| `npm run lint -- src/test/public-static-routes.test.ts` | Passed. |
| `npm run typecheck` | Passed. |
| `CODEX_HOME_DIR=/home/kimwoonggon/.codex POSTGRES_DATA_DIR=/home/kimwoonggon/.woong-blog-docker/dev/postgres BACKEND_PUBLISH_PORT=18080 NGINX_BIND_HOST=127.0.0.1 NGINX_HTTP_PORT=3000 NGINX_HTTPS_PORT=3001 docker compose --env-file .env -f docker-compose.dev.yml -f backend/reports/frontend-test-perfect-completion-2026-05-15/docker-compose.e2e-2core-8gb.override.yml up --build -d db frontend backend nginx` | Passed; stack rebuilt and recreated under the override. |
| `BACKEND_PUBLISH_PORT=18080 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npm run test:e2e:readiness` | Passed: Docker daemon, backend health, and frontend reachable. |
| `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 BACKEND_PUBLISH_PORT=18080 npm run test:e2e` | Passed: 431 passed, 4 skipped, 0 failed, 17.1m. |

Latest E2E latency summary:

- Tests with latency artifacts: 435
- Budget failures: 0
- Warnings: 69
- Generated at: 2026-05-15T06:05:50.055Z

## Risks And Follow-Up

- The full E2E run is slower under the 2core/8GB profile. The slowest tests remain Notion/admin visual flows, with the slowest at about 19.7s. They passed within the current 30s timeout.
- Existing skipped/fixme Playwright tests remain skipped by design. They are not regressions from this work.
- Existing warning noise in Vitest remains, including React act warnings and mocked revalidation warnings. They did not fail the suite, but they are still useful cleanup candidates.

## Final Recommendation

The frontend test state is now clean for the requested gate:

- Vitest is green.
- Core Playwright is green.
- Core Playwright is green under an explicit Docker runtime profile totaling 2core/8GB.

Use the 2core/8GB override as the reproducible local resource gate when this exact constraint matters.
