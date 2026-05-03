# Issue 29 Load Test Dashboard Audit

Generated: 2026-05-03

## Summary

Implemented GitHub issue #29 by adding an admin-only real-time load-test dashboard at `/admin/load-test`.

The dashboard builds default scenarios from 100 to 1000 virtual users in 100-user steps and targets public Work and Study read paths:

- Work list: `/works`
- Work read: first available `/works/{slug}`
- Study list: `/blog`
- Study read: first available `/blog/{slug}`

During a run, the dashboard updates request progress and each scenario row in real time as individual requests complete. Rows expose state, completed request count, p50, p95, average, max, and error rate.

## Changed

- Added shared load-test planning/statistics utilities in `src/lib/load-test-dashboard.ts`.
- Added `src/components/admin/LoadTestDashboard.tsx`.
- Added admin route `src/app/admin/load-test/page.tsx`.
- Added `Load Test` to the admin sidebar navigation.
- Added Vitest coverage for scenario defaults, input clamping, target generation, and percentile/error summaries.
- Added Playwright E2E coverage proving live in-progress results appear before the run completes.
- Updated the dated TODO file with issue mapping and verification notes.

## Intentionally Not Changed

- Did not add a backend/distributed load generator.
- Did not add persistence for load-test results.
- Did not add CI performance gates based on these synthetic measurements.
- Did not modify public Work/Study page rendering behavior.
- Did not touch unrelated local dirty state such as `image.png` or old hotfix/report directories.

## Goal Verification

- Issue was read from GitHub: `#29 부하 테스트 측정`.
- Work was done on `feature/issue-29-load-test-dashboard` from `origin/dev`.
- Dashboard is admin-only through the existing `/admin` layout/session gate.
- Default user steps satisfy the issue requirement: `100, 200, ..., 1000`.
- Real-time behavior is covered by E2E with delayed responses: the table shows `Running` and partial request progress such as `1 / 2` before final `Completed` rows.

## Validations

- `npx skills find playwright e2e github issue`
- `gh issue view 29 --json number,title,body,comments,labels,assignees,url,state`
- `npx vitest run src/test/load-test-dashboard.test.ts`
- `npx vitest run src/test/load-test-dashboard.test.ts src/test/admin-sidebar-nav.test.tsx`
- `npx eslint src/lib/load-test-dashboard.ts src/components/admin/LoadTestDashboard.tsx src/app/admin/load-test/page.tsx src/components/admin/AdminSidebarNav.tsx src/test/load-test-dashboard.test.ts src/test/admin-sidebar-nav.test.tsx tests/admin-load-test-dashboard.spec.ts`
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build frontend nginx`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1 --grep "admin can run"`
- `npm run typecheck`
- `git diff --check`

## Risks And Follow-Up

- This is a browser-driven synthetic read-load dashboard. It is useful for admin-visible latency measurement, but it is not a replacement for distributed server-side load testing.
- The default full scenario can issue many requests. Concurrency is capped to keep the browser and service controlled.
- Results are ephemeral and disappear on refresh. Persisting historical results can be added later if trending is required.

## Recommendation

Proceed with dev push and CI. If dev CI succeeds, promote the same change to main.
