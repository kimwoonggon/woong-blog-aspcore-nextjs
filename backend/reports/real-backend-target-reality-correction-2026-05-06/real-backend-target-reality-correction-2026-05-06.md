# Real Backend Target Reality Correction - 2026-05-06

## Goal
Correct the Real Backend Test workload so it represents current public production content rather than seeded/lightweight fixtures, and improve backend public hot-path query structure without adding cache-based workarounds.

## User Instructions Covered
- Do not force `seeded-work` or `seeded-blog` as Real Backend read targets.
- Do not reduce list targets to `pageSize=1`; keep realistic public list size `pageSize=12`.
- Public Work/Blog list DTOs must include only visible/card fields, not heavy detail content.
- Verify whether public hot paths read all/global data or only necessary DB rows and columns.
- Work/Blog read targets may hit heavy video/text pages because that is realistic backend load.
- No output cache, response cache, CDN cache, or server-side memoization was added.
- Validate through local tests and promote through `dev` and `main` CI.

## Changed
- `src/app/admin/load-test/page.tsx`: removed seeded slug priority. The admin Real Backend page now passes fetched public Work/Blog slug order directly into target generation.
- `src/lib/load-test-dashboard.ts`: restored Real Backend list targets to `/api/public/works?page=1&pageSize=12` and `/api/public/blogs?page=1&pageSize=12`.
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkQueryStore.cs`: removed `ContentJson` from the public Work list card-row projection and thumbnail resolution path. Public Work detail still returns `ContentJson`.
- `backend/src/WoongBlog.Infrastructure/Modules/Composition/Persistence/HomeQueryStore.cs`: changed public home featured works/recent posts from full entity plus global asset reads to card projections plus targeted asset/video lookups.
- `backend/tests/WoongBlog.Api.IntegrationTests/PublicEndpointsTests.cs`: added public API contract assertions that Work/Blog list payloads omit `contentJson`, while Work/Blog detail payloads include `contentJson`.
- `backend/tests/WoongBlog.Api.ComponentTests/PublicQueryHandlerComponentTests.cs`: added a regression test proving home featured work cards do not use body inline images as thumbnail fallback.
- `src/test/load-test-dashboard.test.ts`: added coverage that fetched slug order is preserved and seeded fixtures are not forced ahead of real public content.
- `tests/admin-load-test-dashboard.spec.ts`: updated Real Backend UI expectations to `pageSize=12`.
- `backend/reports/backend-real-load-performance-2026-05-05/*`: marked the old seed/pageSize=1 performance evidence as superseded/invalid for realistic production-load interpretation.
- `backend/reports/backend-real-load-performance-2026-05-05/public-api-k6.js`: removed seeded read defaults. The standalone helper keeps list `pageSize=12` and requires explicit current-content read paths when used outside the UI.
- Deleted tracked old k6 summary JSON artifacts that represented the invalid seed/pageSize=1 workload.

## Query Structure Audit
- Public Work list: `Where(Published)` plus optional search, `OrderBy(PublishedAt)`, `Count`, `Skip/Take`, then `Select` to card fields only. It does not project `ContentJson` or `AllPropertiesJson` for list cards.
- Public Blog list: `Where(Published)` plus optional search, `OrderBy(PublishedAt)`, `Count`, `Skip/Take`, then `Select` to card fields only. It does not project `ContentJson` for list cards.
- Public home featured works: now `Take(3)` and `Select` to card fields only. It no longer reads full Work entities or body JSON for card thumbnails.
- Public home recent posts: now `Take(6)` and `Select` to card fields only. It no longer reads full Blog entities or body JSON for cards.
- Asset lookup on public list/home cards: now uses only referenced asset IDs via `WHERE id IN (...)`; it no longer performs `Assets.ToListAsync()` global reads in `HomeQueryStore`.
- Work video lookup on public home cards: now queries only featured works that need video fallback, and skips the DB query entirely when no work needs it.
- Detail endpoints: intentionally fetch one published row by slug and include `ContentJson`, because read/detail pages must render full content.

## 2CPU/8GB Prod-like Evidence
The valid local prod-like stack constrained backend, frontend, db, and nginx to the same 2 CPU cores and about 7.5GB aggregate container memory. The k6 runner was pinned outside those 2 cores so it did not consume the simulated server CPU budget.

- Constant 100rps, 30s, `pageSize=12`, current heavy read targets: 3,001 requests, 100.0 rps, 0 failures, p95 11.3ms, max 56.8ms, dropped iterations 0.
- Spike 100 -> 1000 -> 100 rps, 60s, maxVUs 1000, `pageSize=12`, current heavy read targets: 26,403 requests, avg 330.0 rps, failure rate 3.52%, p95 3,653.5ms, max 4,981.9ms, dropped iterations 12,596.
- Interpretation: current backend is healthy around 100rps on the constrained stack, but 1000rps spike is beyond current total system capacity. Raising maxVUs exposes saturation; it does not create backend capacity.

## Intentionally Not Changed
- No cache was added. Cache remains a later option, not the primary fix.
- No attempt was made to make heavy Work/Blog detail content artificially small. Heavy video/text detail pages remain valid Real Backend read targets.
- Public Work list and home featured work no longer use content-body image fallback for thumbnail resolution because that would require selecting heavy `ContentJson` on card/list paths. Explicit thumbnail assets and video fallback remain available.
- Admin list/detail paths were not optimized in this slice because the user concern and load test path are public Real Backend endpoints.

## Verification Against Goals
- Real Backend read target selection uses first slug from fetched public Work/Blog pages without seeded override.
- Real Backend list targets are back to `pageSize=12`.
- Public list DTOs are covered by integration tests to exclude `contentJson`.
- Public detail DTOs are covered by integration tests to include `contentJson`.
- Public list/home query stores now avoid global asset reads and avoid list/card-unneeded body JSON.
- Cache avoidance was checked by reviewing the changed files and preserving the no-cache approach.

## Local Validations Performed
- RED: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests.GetHomeQueryHandler_DoesNotUseWorkBodyImageAsFeaturedThumbnailFallback" --no-restore` failed before implementation because the old home query used body inline image fallback.
- GREEN: same focused test passed after `HomeQueryStore` switched to card projection plus targeted asset/video lookup.
- PASS: `npm test -- --run src/test/load-test-dashboard.test.ts` - 27/27 tests passed.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests" --no-restore` - 27/27 tests passed. Existing `AWSSDK.Core` NU1901 warnings remain.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` - 35/35 tests passed. Existing `AWSSDK.Core` NU1901 warning remains.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` - Component 117/117, Unit 56/56, Architecture 35/35, Integration 202/202, Contract 1 skipped. Existing `AWSSDK.Core` NU1901 warnings remain.
- PASS: `npm run test -- --run` - 82 test files and 589 tests passed.
- PASS: `npm run typecheck`.
- PASS: `npm run lint` - 0 errors, 6 existing unrelated warnings.
- PASS: `git diff --check`.
- PASS: `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` - backend publish and Next production build completed, dev compose stack started on `127.0.0.1`.
- PASS: `curl -sS -o /dev/null -w '%{http_code} %{time_total}\n' http://127.0.0.1:3000/api/health` returned HTTP 200.
- PASS: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1` - 2/2 tests passed.

## CI Promotion Status
- Previous correction commit `118aa4c` reached `dev` through PR #41 and `CI Dev` run `25389581624` passed.
- Previous automatic `main` promotion PR #42 merged to `main`; `CI Main Runtime` PR run `25389979871` passed.
- Previous `main` push run `25390322125` initially hit an unrelated frontend unit-test flake, then passed after rerunning failed jobs.
- Current home query-shape correction still needs a new PR into `dev`, `dev` CI, automatic `main` promotion, and `main` CI verification.

## Risks And Yellow Flags
- Removing `ContentJson` from public Work list/home card paths means thumbnail discovery from inline body images is intentionally not available on card/list responses. Content relying only on inline images should use explicit thumbnail assets or video preview fallback.
- Existing `AWSSDK.Core` NU1901 warnings remain unrelated to this correction.
- The old 2026-05-05 k6 numbers are explicitly invalid for realistic workload interpretation because they used seed/pageSize=1 targets.
- The 1000rps spike result is still red on the constrained 2CPU/8GB-total profile. Further work should target detail endpoint CPU/allocation/serialization cost and database concurrency before considering cache.

## Recommendation
Treat the backend as structurally improved for public list and home card paths, but not yet capable of a 1000rps spike on the stated 2CPU/8GB-total deployment profile. Next capacity work should optimize heavy detail rendering/serialization and DB concurrency using the corrected Real Backend Test workload: `pageSize=12`, current public read targets, no seeded priority, no cache masking.
