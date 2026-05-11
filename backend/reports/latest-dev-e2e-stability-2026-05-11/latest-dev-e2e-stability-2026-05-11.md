# Latest Dev E2E Stability Fix - 2026-05-11

## Summary

Fixed two dev full-E2E blockers found after validating the latest dev runtime:

- Removed the seeded `mailto:woong@example.com` contact link that violated the public contact fallback guard.
- Made the public blog edge-navigation E2E deterministic by seeding enough published blog records for a page-2 next link and explicitly revalidating the public `/blog` page cache after backend API fixture creation.

## Goals Verified

- Dev full E2E should pass without considering production SSH or remote server state.
- Homepage/public navigation/admin/media flows should remain functional in the Docker-based dev environment.
- Real Backend Test smoke should still run against `pageSize=12` public Work/Study list targets and non-seed/non-fixture read URLs.

## Non-Goals

- No production SSH, server pull, or remote deployment was performed.
- No production compose/env configuration was changed.
- No caching strategy was added as a performance shortcut.
- No backend query/performance architecture change was included in this slice.

## Changes

- `backend/src/WoongBlog.Infrastructure/Persistence/Seeding/SeedData.cs`
  - Contact seed content no longer exposes `mailto:woong@example.com`.
- `tests/public-edge-nav.spec.ts`
  - Adds a deterministic blog-page fixture setup for the edge navigation test.
  - Calls `/revalidate-public` after backend API fixture creation so the Next public page cache matches the public API state.
  - Keeps the public list test on the realistic desktop `pageSize=12` contract.
- `todolist-2026-05-11.md`
  - Records the latest dev validation, failures, fixes, and verification results.

## Validation

- Targeted E2E on existing dev stack `http://127.0.0.1:3002`: 4 passed.
- `npm run typecheck`: passed.
- `dotnet test backend/WoongBlog.sln --verbosity minimal`: passed.
  - Contract: 1 skipped.
  - Component: 130 passed.
  - Unit: 61 passed.
  - Architecture: 40 passed.
  - Integration: 231 passed.
  - Known warning: NU1901 low severity `AWSSDK.Core` advisory.
- Fresh dev compose stack `latest-dev-e2e-stability-20260511` on `http://127.0.0.1:3004`: health 200.
- Targeted E2E on fresh dev stack: 4 passed.
- Full E2E on fresh dev stack: 430 passed, 4 skipped.
  - Latency artifacts: 434.
  - Budget failures: 0.
  - Warnings: 13.
- Real Backend Test smoke on fresh dev stack:
  - Run ID: `20260511-025656-public-api-rps-715ff2ae`.
  - Status: completed.
  - Requests: 501.
  - Failed requests: 0.
  - Average RPS: 50.1.
  - P95: 2.2 ms.
  - P99: 4.2 ms.
  - Diagnostics samples: 12.
  - Targets: `pageSize=12` Work/Study lists, `/api/public/works/internal-admin-workbench`, `/api/public/blogs/engineering-notes-on-bff-auth`.

## Risks And Follow-Ups

- The fresh dev compose runtime reports 8 processors and no local CPU/memory quota, so these results validate dev behavior, not the user's 2 CPU / shared 8 GiB production profile.
- The full suite mutates the dev database heavily; load-test target selection should continue to avoid `seed`, `fixture`, and Playwright-specific slugs when interpreting performance.
- Production public origin remains externally blocked until the server pulls and runs the main images; no active-goal completion is claimed here.

## Recommendation

Promote this E2E stability fix through dev CI, then main, because it removes a real seed-data mismatch and fixes a deterministic-test issue without changing production runtime behavior.
