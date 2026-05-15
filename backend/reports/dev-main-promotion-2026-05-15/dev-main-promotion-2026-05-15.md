# Dev/Main Promotion Audit - 2026-05-15

## Scope

This audit records the pre-push state for promoting the validated UI/frontend-test work toward `dev`, then `main` after CI gates pass.

## Changed

- Added backend public detail context endpoints for blogs and works.
- Added bounded detail context query/DTO/handler paths and query-store methods.
- Updated public blog/work detail pages to use bounded context for related/adjacent content instead of fetching full public lists.
- Updated static param tests to match current paginated public list behavior.
- Updated public home featured works to a six-card, three-column desktop layout.
- Updated public search to live debounce URL updates without `searchMode`.
- Added/updated direct Vitest, xUnit, and Playwright coverage for the above behavior.
- Added the strict 2core/8GiB E2E validation report artifacts from the frontend test completion work.

## Intentionally Not Changed

- Did not include rollback-like dirty-tree diffs for production redeploy workflow evidence bundling.
- Did not include Dockerfile SWC compatibility rollback.
- Did not include runtime allowlist or production preflight rollbacks.
- Did not include database schema patch rollback.
- Did not include the dirty-tree TOC component accessibility rollback.
- Did not push the stale primary worktree directly.

## Goal Verification

- `origin/dev` did not contain the detail context implementation used by the validated frontend tests.
- A clean worktree was created from `origin/dev` and only the scoped source/test/report/TODO files were applied.
- The candidate branch is `codex/frontend-test-perfect-completion-20260515`.
- The dev CI gate is still pending because the branch has not yet been pushed at the time of this report.
- Main promotion remains blocked until dev CI is green.

## Local Validations

- `./node_modules/.bin/vitest run src/test/public-static-routes.test.ts src/test/blog-detail-related.test.tsx src/test/work-detail-related-order.test.tsx src/test/public-api-clients.test.ts src/test/public-detail-boundary.test.tsx src/test/public-home-date-fallback.test.tsx src/test/public-search-form.test.tsx --pool=threads --maxWorkers=2 --reporter=dot`
  - Passed: 7 files, 40 tests.
- `npm run lint -- 'src/app/(public)/blog/[slug]/page.tsx' 'src/app/(public)/works/[slug]/page.tsx' 'src/app/(public)/page.tsx' src/components/layout/PublicSearchForm.tsx src/lib/api/blogs.ts src/lib/api/works.ts src/test/public-static-routes.test.ts src/test/public-search-form.test.tsx`
  - Passed.
- `npm run typecheck`
  - Passed.
- `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~PublicQueryHandlerComponentTests`
  - Passed: 43 tests.
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter FullyQualifiedName~PublicEndpointsTests`
  - Passed: 33 tests.
- `npm test`
  - Passed: 93 files, 653 tests, 1329.62s.
- `dotnet test backend/WoongBlog.sln`
  - Passed: component 133, unit 61, architecture 40, integration 234, contract 1 skipped.
  - Warning: existing NU1901 low-severity `AWSSDK.Core` advisory.
- `git diff --check`
  - Passed.

## Risks And Follow-Ups

- Dev and main CI results are not recorded yet.
- Full Docker E2E under the explicit 2core/8GiB profile was already passed before this promotion candidate was isolated, but has not been rerun from the clean worktree.
- Existing frontend test warning noise remains, including React `act` warnings and jsdom unsupported `scrollTo` output.

## Recommendation

Proceed with pushing the scoped branch and opening the `dev` gate PR. Do not merge or promote to `main` until GitHub CI is green.
