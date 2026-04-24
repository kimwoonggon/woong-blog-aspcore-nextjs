# Frontend Follow-Up Audit - 2026-04-23

## Summary

Follow-up work addressed the remaining concerns from the broad frontend refactor:
- public route static/cache boundaries
- Study/Works cold-load and pagination timing
- full E2E validation from final state
- multi-iteration origin/dev performance comparison

## Changes

- Added `NEXT_PUBLIC_SITE_URL` support to `getPublicServerApiBaseUrl()` so public server fetches can avoid request-header fallback when the public origin is configured.
- Added `NEXT_PUBLIC_SITE_URL` to `.env.example`, `.env.staging.example`, `.env.prod.example`, `docker-compose.staging.yml`, and `docker-compose.prod.yml`; production example now uses `https://woonglab.com`.
- Moved local-only QA query behavior for `/`, `/introduction`, and `/resume` into small client boundaries instead of server `searchParams`.
- Added `generateStaticParams()` for `/blog/[slug]` and `/works/[slug]`.
- Moved public detail next/previous query preservation into a client link component, removing server `searchParams` from detail pages.
- Dynamically imported heavy admin editor/create components behind public admin gates.
- Fixed the benchmark pagination click timing race by waiting for URL after click.
- Fixed E2E infra waits that used `networkidle` on pages that may keep background requests alive:
  - `tests/responsive-width-sweep.spec.ts`
  - `tests/helpers/ui-improvement.ts`
  - `tests/dark-mode.spec.ts`

## Route Classification

With an explicit public/internal API origin:

```bash
INTERNAL_API_ORIGIN=http://127.0.0.1:18080 NEXT_PUBLIC_SITE_URL=https://woonglab.com npm run build
```

the public output improved to:
- Static ISR: `/`, `/contact`, `/introduction`, `/resume`, `/robots.txt`, `/sitemap.xml`
- SSG ISR: `/blog/[slug]`, `/works/[slug]`
- Still dynamic by design: `/blog` and `/works` because search/pagination are server-rendered from query params.

In the default Docker dev build without build-time API origin, some pages remain dynamic because the backend is not reachable from the image build context; runtime still uses `INTERNAL_API_ORIGIN=http://backend:8080`.

## Validation

- Focused Vitest:
  - `local-qa-query-boundary`, `public-api-clients`, `public-admin-rendering`, `public-detail-boundary`, `responsive-page-size-sync`, and benchmark self-test suites passed.
- Typecheck:
  - `npm run typecheck` passed.
- Build:
  - Explicit-origin `npm run build` passed and showed the static/SSG route improvements above.
  - Docker frontend rebuild passed.
- Focused Playwright:
  - QA query behavior, pagination, related-page retention, inline public editors, loading, and layout shift: 37 passed.
  - Responsive sweep after wait fix: 1 passed in 33.4s.
  - Final failed full-run specs targeted rerun: 3 passed.
- Full E2E:
  - Initial reruns exposed `networkidle` waits and pagination URL-wait races.
  - Final full rerun: 569 passed, 8 skipped, 0 failed.
  - Latency artifacts: 575; latency budget failures: 0; warnings: 84.
- Benchmark:
  - `node scripts/benchmark-frontend-performance.mjs --warmups 1 --iterations 3 --mutation-warmups 1 --mutation-iterations 3 ...` completed.
  - Reports regenerated:
    - `backend/reports/frontend-performance-origin-dev-vs-current/frontend-performance-origin-dev-vs-current.md`
    - `backend/reports/frontend-performance-origin-dev-vs-current/frontend-performance-origin-dev-vs-current.html`
    - `backend/reports/frontend-performance-origin-dev-vs-current/frontend-performance-origin-dev-vs-current.json`

## Benchmark Results

Important improvements:
- Admin blog/work/resume mutation-to-public-visible remains dramatically improved, about 98-99% faster than `origin/dev`.
- Study/Works warm browser navigation improved.
- Contact and resume route behavior improved or stayed neutral.
- AI Fix dialog has no runtime regression and current exposes both OpenAI and Codex.

Remaining performance concerns:
- Study and Works cold first load still regress in the benchmark.
- Some public API endpoint timings regress by small absolute amounts but high percentages because baseline numbers are only a few milliseconds.
- Study direct page 2 p95 still flags as a regression, although focused Playwright page 2 navigation passes.

## Risks

- Full E2E is now green in the final full run; remaining risk is runtime cost, because the full suite still takes about 13 minutes on this machine.
- Static/SSG output depends on build-time API origin availability. Without explicit build-time origin, Docker image builds cannot prerender data-backed public pages.
- Public list pages `/blog` and `/works` remain dynamic by design because they use server-side query search/pagination.

## Recommendation

Accept the follow-up changes. Full E2E is green. Schedule a separate performance pass specifically for Study/Works cold first load and backend public API endpoint latency.
