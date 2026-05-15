# Public Detail UI Pagination Audit - 2026-05-15

## Scope

Requested outcome:

- Make the Work/Study detail right-side table-of-contents rail wider.
- Keep Work/Study detail reading surfaces plain white in light mode.
- Fix Study detail related pagination so the current item can be centered with left/right context instead of only exposing pages 1 and 2.
- Run Docker-backed full E2E before promotion.
- Push through dev CI, then main CI/publish if green.

Base branch: `origin/dev` at `d0a4e42`.

Working branch: `codex/public-detail-ui-pagination-20260515`.

## Changed

- Updated Study and Work detail pages to use a wider desktop TOC rail and a slightly wider detail page shell so the rail does not squeeze the reading column.
- Added explicit light-mode white detail shells for Study and Work while preserving dark-mode backgrounds.
- Increased public detail related context from 9 to 24 items on the frontend and backend.
- Changed backend related-context selection to balance newer and older neighbors around the current item, with fill-in from the other side when one side has fewer items.
- Fixed `RelatedContentList` initial page math so large page sizes can center the current item instead of biasing to pages 1 and 2.
- Added/updated focused Vitest, xUnit, and Playwright coverage for related pagination, detail context ordering, white detail shells, and readable TOC rail width.
- Added this audit artifact set and updated `todolist-2026-05-15.md`.

## Intentionally Not Changed

- No unrelated dirty changes from the primary worktree were included.
- No production secrets, local certificates, Playwright storage state, Docker runtime data, or generated media were added.
- No broad UI redesign was performed beyond the requested detail-page shell/rail/pagination behavior.
- No package upgrades or dependency remediation were attempted.

## Verification

- Backup created under `.agent-backups/public-detail-ui-pagination-2026-05-15/` before implementation edits.
- Focused Vitest RED confirmed new expectations initially failed; final focused Vitest passed 4 files / 16 tests.
- Focused backend component xUnit passed 2 tests.
- Focused backend integration xUnit passed 2 tests.
- Targeted ESLint passed for changed frontend/test files.
- `npm run typecheck` passed after the final layout capacity adjustment.
- Full `npm test` passed 93 files / 654 tests after the behavior/backend changes; the final follow-up was a two-line CSS capacity adjustment covered by focused Playwright and typecheck.
- Full `dotnet test backend/WoongBlog.sln` passed: component 133, unit 61, architecture 40, integration 234, contract 1 skipped.
- Docker dev stack readiness passed:
  - Backend: `http://127.0.0.1:18082/api/health`
  - Frontend: `http://127.0.0.1:13002`
- Focused Playwright after the final layout adjustment passed:
  - `tests/ui-improvement-related-content-width.spec.ts`
  - `tests/ui-quality-blog-detail-visuals.spec.ts`
  - Result: 7 passed.
- Final Docker-backed core full E2E passed:
  - Command: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:13002 BACKEND_PUBLISH_PORT=18082 npm run test:e2e`
  - Result: 432 passed, 4 skipped, 0 failed, 15.5m.
  - Latency artifacts: 436.
  - Budget failures: 0.
  - Warnings: 81.

## Environment Notes

- Local Docker daemon reported 8 CPUs and 67,364,700,160 bytes of memory.
- The strict 2 core / 8 GB Docker limit was not proven in this local run.
- Default local ports were unavailable in this environment:
  - `127.0.0.1:18080` was already listening.
  - nginx `3000` was already allocated.
  - nginx `3002` failed Docker Desktop port exposure.
- The successful local dev stack used backend port `18082` and nginx HTTP port `13002`.
- `NGINX_DEFAULT_CONF=./nginx/default.conf` was used because the default `local-https.conf` requires `.local-certs/localhost.pem`, which was not present.

## Risks And Yellow Flags

- Existing npm audit output reported vulnerabilities during dependency install; dependency remediation was outside this request.
- Existing `dotnet test` output included an NU1901 low-severity warning for `AWSSDK.Core`; remediation was outside this request.
- E2E latency warnings increased to 81, but budget failures remained 0.
- The local 2 core / 8 GB constraint still needs a separate constrained-resource proof if that remains a hard release gate.

## Recommendation

Proceed to commit, push this branch, open a PR into `dev`, and wait for dev CI. Promote to `main` only after the dev PR and dev branch checks are green.
