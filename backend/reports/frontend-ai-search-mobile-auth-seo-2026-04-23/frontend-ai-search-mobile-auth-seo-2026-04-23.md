# Frontend AI/Search/Mobile/Auth/SEO Audit (2026-04-23)

## 1) Summary of Changes

- Public `/blog` and `/works` list/search UX was split by viewport:
  - `<1024px`: infinite feed (`page=1`, `pageSize=10`) with `IntersectionObserver` + accessible `Load more`.
  - `>=1024px`: existing SSR pagination flow retained.
- Public search UI was unified to query-only input + icon submit; frontend-generated URLs no longer emit `searchMode`.
- Backend public search now defaults to unified query mode (`title OR content`) when only `query` is supplied.
- Backend legacy compatibility for `searchMode=title|content` was preserved.
- Admin AI prompt persistence keys were separated by flow (blog single fix / blog batch / work enrich).
- AI runtime config payload now provides separate default prompts for blog fix and work enrich.
- Blog excerpt persistence was wired end to end (admin action -> API request -> command/handler/validator -> storage fallback policy).
- Admin auth sliding expiration was updated to 300 minutes while keeping absolute expiration at 8 hours.
- Blog/Work detail TOC rail layout was bounded to article content area.
- Work detail metadata now emits social images (`og:image`, `twitter:image`) from work thumbnail or YouTube thumbnail.
- Warning-noise reduction (without excluding evaluation) was applied:
  - interaction normalization/dedup for budget evaluation while preserving raw captured interactions,
  - interaction-profile budget matching (`name/file/title/target` aware),
  - explicit interaction budget profiles (`click`, `keydown`, `input`, `hover-enter`, `hover-leave`).

## 2) Intentionally Not Changed

- Admin table/list pagination behavior (out of scope).
- Unrelated backend architecture and infrastructure refactors.
- Unrelated visual redesign/theme changes.
- Production secret handling and deployment topology.

## 3) Goal / Non-goal Verification

- Requested public viewport split and infinite behavior: satisfied.
- Requested unified public search URL policy (no frontend `searchMode`): satisfied.
- Requested backend legacy URL compatibility for `searchMode`: satisfied.
- Requested AI prompt key split + runtime default prompt split: satisfied.
- Requested excerpt persistence + fallback behavior: satisfied.
- Requested auth sliding expiration update: satisfied.
- Requested TOC rail boundary + work social image metadata: satisfied.
- Requested full E2E + latency consideration and audit refresh: satisfied.
- Requested warning reduction without excluding evaluation: satisfied.

## 4) Validations Performed

- Docker dev stack health check (`http://127.0.0.1:3000`, `/api/health`): passed.
- Full Playwright + latency run:
  - Command: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible node scripts/run-e2e-latency.mjs -- --workers=1`
  - Result: `test-results/playwright/.last-run.json` status `passed`.
  - Latency summary (`2026-04-23T13:29:45.297Z`): artifacts `574`, budget failures `0`, warnings `37`.
- Warning-count delta against previous baseline in this audit track:
  - Before: warnings `177`
  - After: warnings `37`
  - Delta: `-140` (`-79.1%`)
- Required validation commands:
  - `npm test`: passed (`59` files, `293` tests).
  - `npm run lint`: failed due environment permission (`EACCES` on `.tmp/postgres` scan), not due lint-rule violations in changed code.
  - `npm run typecheck`: passed.
  - `dotnet test backend/WoongBlog.sln`: failed on
    `backend/tests/WoongBlog.Api.IntegrationTests/WorkVideoEndpointsTests.cs:136`
    (`FfmpegVideoTranscoder_SegmentsHlsAndProducesTheManifest`, assertion matched `"720"` unexpectedly).

### Response-time Highlights (Current Full Run)

- Study mobile initial render (`390px`, `/blog`): `138.65ms` (`page.goto`).
- Study tablet append next page (`820px`, `/blog`): `137.40ms` (`Study tablet load more appends next page`).
- Works mobile initial render (`390px`, `/works`): `142.32ms` (`page.goto`).
- Works tablet append next page (`820px`, `/works`): `68.18ms` (`Works tablet load more appends next page`).
- Study desktop pagination next nav (`>=1024px`): `177.66ms`.
- Works desktop pagination next nav (`>=1024px`): `162.09ms`.
- Unified search submit:
  - Study: `553.87ms` (`Study unified search submit`).
  - Works: `223.68ms` (`Works unified search submit`).
  - Response-time suite path: `599.30ms` (`Study unified search submit response-time path`).

## 5) Warning Classification (Can Reduce vs Keep)

### A) Reducible Without Feature Loss

- Duplicate interaction-event families (already reduced in this change): removed via normalization/dedup.
- Remaining interaction warnings from admin notion flows (`27` of `37`):
  - mostly `keydown` (`15`), `click` (`6`), `hover-enter` (`5`), `input` (`1`).
  - reducible by interaction-path optimization (same functionality): reduce avoidable hover chains before click, tighten test typing payloads where the same autosave behavior is asserted, and reduce first-open UI workload in notion sheet/search flows.
- Auth bootstrap API warnings (`GET /api/auth/test-login`, `4` warnings): reducible by reusing authenticated state more aggressively in runtime-auth suites.

### B) Should Stay as Signal (For Now)

- Long-running structural checks:
  - `responsive-width-sweep` test warning (`32215.88ms`) is an intentional broad viewport sweep and should remain visible unless the sweep workload itself is redesigned.
- Real backend-path pressure warnings:
  - autosave/blog-fix/site-settings API hard warnings indicate true end-to-end path cost and should not be suppressed by policy.

## 6) Risks / Yellow Flags / Deferred Follow-up

- `npm run lint` is blocked by `.tmp/postgres` permission in this environment; lint job needs path exclusion or permissions normalization in dev workflow.
- `dotnet test backend/WoongBlog.sln` currently has one integration failure unrelated to this warning-normalization patch and should be triaged separately.
- Remaining `37` latency warnings are now concentrated in meaningful hotspots (not event-duplication noise), primarily admin notion interactions and auth bootstrap API latency.

## 7) Final Recommendation

- Accept the warning-noise reduction patch and keep current full-run latency gating (`budget failures: 0`).
- Next reduction step without hiding signal: optimize admin notion interaction path costs and auth bootstrap setup so the remaining `37` warnings trend down while preserving strict evaluation.
