# Production-Like Latency Audit (production-like-latency-2026-04-26)

## Scope
- User requested production-like latency measurement after local click behavior felt slower.
- Measurement intentionally excluded `next dev` and used the Docker production frontend image, backend/db compose services, and nginx routing for `/api` and `/media`.
- Primary target: updated Playwright response-time spec covering public nav, Work/Study detail card opens, mobile append, search, and selected authenticated admin paths.

## Environment
- Frontend: Docker production build from current workspace, Next.js 16.1.6 standalone runtime.
- Backend/db: existing `docker-compose.dev.yml` backend and Postgres services.
- Proxy: temporary nginx container on `woong-blog-aspcore-nextjs_default` network using `nginx/local-https.conf`.
- Browser runner: `mcr.microsoft.com/playwright:v1.58.2-noble` with `PLAYWRIGHT_BASE_URL=http://<temporary-nginx-container>`.

## Commands
- `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml build frontend`
- `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d frontend`
- `docker run -d --name <nginx> --network woong-blog-aspcore-nextjs_default -v $PWD/nginx/local-https.conf:/etc/nginx/conf.d/default.conf:ro -v $PWD/.local-certs:/etc/nginx/certs:ro nginx:1.27-alpine`
- `docker run --rm --network woong-blog-aspcore-nextjs_default -v $PWD:/work -w /work -e PLAYWRIGHT_EXTERNAL_SERVER=1 -e PLAYWRIGHT_BASE_URL=http://<nginx> -e PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible mcr.microsoft.com/playwright:v1.58.2-noble bash -lc "node scripts/run-e2e-latency.mjs -- tests/e2e-response-time.spec.ts --workers=1"`

## Results
- Pass 2: 9/9 passed, budget failures 0, warnings 1.
- Pass 3: 9/9 passed, budget failures 0, warnings 0.
- Preserved artifacts:
  - `backend/reports/production-like-latency-2026-04-26/artifacts/pass-2-summary.json`
  - `backend/reports/production-like-latency-2026-04-26/artifacts/pass-2-summary.md`
  - `backend/reports/production-like-latency-2026-04-26/artifacts/pass-3-summary.json`
  - `backend/reports/production-like-latency-2026-04-26/artifacts/pass-3-summary.md`

## Pass 2 Slowest Measured Steps
| Duration ms | Status | Step |
| --- | --- | --- |
| 1137 | passed | Public nav click to Study |
| 949 | passed | Public nav click to Works |
| 675 | passed | Public Work card opens detail |
| 654 | passed | Public Study card opens detail |
| 585 | passed | Study list direct load to primary content visible |
| 524 | passed | Public nav click to Introduction |
| 439 | passed | Public nav click to Contact |
| 432 | passed | Admin site settings save response-time path |
| 402 | passed | Works list direct load to primary content visible |
| 323 | passed | Study unified search submit response-time path |
| 170 | passed | Works mobile auto-append |
| 166 | passed | Study mobile auto-append |

## Pass 2 Warnings
- Public nav click to Study: 1137.2ms over warn 1000ms, hard 1500ms.

## Pass 3 Slowest Measured Steps
| Duration ms | Status | Step |
| --- | --- | --- |
| 842 | passed | Public nav click to Works |
| 582 | passed | Study unified search submit response-time path |
| 565 | passed | Public Study card opens detail |
| 565 | passed | Study list direct load to primary content visible |
| 522 | passed | Public nav click to Introduction |
| 521 | passed | Admin site settings save response-time path |
| 503 | passed | Public Work card opens detail |
| 477 | passed | Public nav click to Contact |
| 460 | passed | Public nav click to Study |
| 394 | passed | Works list direct load to primary content visible |
| 219 | passed | AI Fix provider dropdown response-time path |
| 180 | passed | Study mobile auto-append |

## Pass 3 Warnings
None.

## Validation
- Docker frontend production build succeeded with Next.js 16.1.6; production server logged ready in 79ms.
- Internal nginx health probe returned HTTP 200 with time_total about 0.03s.
- Production-like latency pass-2: 9/9 passed, budget failures 0, warnings 1.
- Production-like latency pass-3: 9/9 passed, budget failures 0, warnings 0.
- Latest preserved latency artifacts are under backend/reports/production-like-latency-2026-04-26/artifacts/.

## Intentionally Not Changed
- No production application code was changed for this measurement pass.
- No performance budgets were changed.
- No full e2e suite run was started because the requested goal was production-like latency measurement and the targeted latency spec already covers the reported public nav/detail paths.

## Risks And Yellow Flags
- Host port publishing for a temporary nginx container failed in Docker Desktop/WSL with a 500 from /forwards/expose, so measurement used Docker internal networking instead of a Windows browser hitting localhost.
- This measured the targeted response-time Playwright slice, not the entire 589-test Playwright inventory.
- One preserved pass had a soft warning for Study nav at 1137.2ms over a 1000ms warning budget; the next pass had zero warnings and the hard budget never failed.
- The official Playwright image was pulled for this run; image pull time is not part of the measured app latency.

## Recommendation
Treat the reported local click slowness as next dev cold compilation noise unless it reproduces in the production-like Docker runtime. The production-like targeted latency slice is currently acceptable: repeated runs passed with zero hard budget failures; watch Study nav because it showed one soft warning sample.
