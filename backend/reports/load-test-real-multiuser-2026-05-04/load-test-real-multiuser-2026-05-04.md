# Load Test Real Multiuser Dashboard Audit

Generated: 2026-05-04

## Summary

Updated the admin load-test dashboard so its default workload targets backend public API endpoints instead of cached public page routes.

The dashboard now models each planned user as a distinct virtual user by adding request identity query parameters to every load-test request:

- `__loadTestRun`
- `__loadTestUser`
- `__loadTestRequest`
- `__loadTestIteration`

Work/Study target URLs are editable directly in the dashboard, including Work read and Study read, so production target paths/domains can be changed without code edits.

## Changed

- Changed default Work targets from `/works` and `/works/{slug}` to `/api/public/works?page=1&pageSize=12` and `/api/public/works/{slug}`.
- Changed default Study targets from `/blog` and `/blog/{slug}` to `/api/public/blogs?page=1&pageSize=12` and `/api/public/blogs/{slug}`.
- Added per-target URL inputs in `LoadTestDashboard`.
- Changed load-test fetches to omit admin cookies, so public API tests behave more like anonymous users.
- Added virtual-user identity query parameters per request.
- Updated Vitest and Playwright coverage for the new behavior.

## Intentionally Not Changed

- Did not add a server-side distributed load generator.
- Did not add k6/JMeter integration.
- Did not persist load-test results.
- Did not change public API caching implementation.
- Did not modify backend endpoint behavior.
- Did not touch unrelated dirty files such as `image.png` or older hotfix/report directories.

## Goal Verification

- Backend load is now targeted directly through `/api/public/...` by default.
- Work read and Study read target URLs are editable from the admin dashboard.
- Each virtual user receives a distinct `__loadTestUser` identity.
- Each request receives a unique `__loadTestRequest` identity.
- Existing live progress updates remain intact.

## Validations

- `npx skills find "load testing docker backend k6 playwright"`
- `npx vitest run src/test/load-test-dashboard.test.ts`
- `npx eslint src/lib/load-test-dashboard.ts src/components/admin/LoadTestDashboard.tsx src/test/load-test-dashboard.test.ts tests/admin-load-test-dashboard.spec.ts`
- `npm run typecheck`
- `BACKEND_PUBLISH_PORT=18080 NGINX_HTTPS_PORT=3002 docker compose -f docker-compose.dev.yml up -d --build frontend nginx`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_E2E_PROFILE=exhaustive npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1 --grep "admin can run"`

## Risks And Follow-Up

- Browser-driven load generation is still not equivalent to a distributed load test. Browser connection limits can cap true simultaneous connections.
- This change makes backend API load visible and avoids the previous frontend-page-cache bias, but production-grade multi-node load testing should still use k6 or another external load generator.
- Absolute cross-origin target URLs may require CORS support from the target domain.

## Recommendation

Use the dashboard for admin-visible backend API read-load checks and Docker stats observation. For production capacity decisions, add a server-side or k6-based load runner as a separate feature.
