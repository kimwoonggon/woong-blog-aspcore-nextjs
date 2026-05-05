# Real Backend Target Reality Correction - 2026-05-06

## Goal
Correct the Real Backend Test workload so it represents current public production content rather than seeded/lightweight fixtures, while keeping public list endpoints limited to visible card fields only.

## User Instructions Covered
- Do not force `seeded-work` or `seeded-blog` as Real Backend read targets.
- Do not reduce list targets to `pageSize=1`; keep realistic public list size `pageSize=12`.
- Public Work/Blog list DTOs must include only visible/card fields, not heavy detail content.
- Work/Blog read targets may hit heavy video/text pages because that is realistic backend load.
- No cache-based workaround was added.
- Complete local validation and proceed through dev/main CI promotion.

## Changed
- `src/app/admin/load-test/page.tsx`: removed seeded slug priority. The admin Real Backend page now passes fetched public Work/Blog slug order directly into target generation.
- `src/lib/load-test-dashboard.ts`: restored Real Backend list targets to `/api/public/works?page=1&pageSize=12` and `/api/public/blogs?page=1&pageSize=12`.
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkQueryStore.cs`: removed `ContentJson` from the public Work list card-row projection and thumbnail resolution path. Public Work detail still returns `ContentJson`.
- `backend/tests/WoongBlog.Api.IntegrationTests/PublicEndpointsTests.cs`: added public API contract assertions that Work/Blog list payloads omit `contentJson`, while Work/Blog detail payloads include `contentJson`.
- `src/test/load-test-dashboard.test.ts`: added coverage that fetched slug order is preserved and seeded fixtures are not forced ahead of real public content.
- `tests/admin-load-test-dashboard.spec.ts`: updated Real Backend UI expectations to `pageSize=12`.
- `backend/reports/backend-real-load-performance-2026-05-05/*`: marked the old seed/pageSize=1 performance evidence as superseded/invalid for realistic production-load interpretation.
- `backend/reports/backend-real-load-performance-2026-05-05/public-api-k6.js`: removed seeded read defaults. The standalone helper keeps list `pageSize=12` and requires explicit current-content read paths when used outside the UI.
- Deleted tracked old k6 summary JSON artifacts that represented the invalid seed/pageSize=1 workload.

## Intentionally Not Changed
- No output cache, response cache, CDN cache, or server-side memoization was added.
- No attempt was made to normalize heavy Work/Blog detail content. Heavy video/text detail pages remain valid Real Backend read targets.
- Blog list query shape was not changed because it already projected only card fields and did not select `ContentJson`.
- Public Work list no longer uses content-body image fallback for thumbnail resolution because that would require projecting heavy `ContentJson` into the list query. Explicit thumbnail assets and video fallback remain available.

## Verification Against Goals
- Real Backend read target selection now uses first slug from fetched public Work/Blog pages without seeded override.
- Real Backend list targets are back to `pageSize=12`.
- Public list DTOs are covered by integration tests to exclude `contentJson`.
- Public detail DTOs are covered by integration tests to include `contentJson`.
- Cache avoidance was checked by reviewing the changed files and preserving the no-cache approach.

## Local Validations Performed
- PASS: `npm test -- --run src/test/load-test-dashboard.test.ts` - 27/27 tests passed.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests" --no-restore` - 27/27 tests passed. Existing `AWSSDK.Core` NU1901 warnings remain.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` - 34/34 tests passed. Existing `AWSSDK.Core` NU1901 warning remains.
- PASS: `npm run typecheck`.
- PASS: `npm run lint` - 0 errors, 6 existing unrelated warnings.
- PASS: `git diff --check`.
- PASS: `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh` - backend publish and Next production build completed, dev compose stack started on `127.0.0.1`.
- PASS: `curl -sS -o /dev/null -w '%{http_code} %{time_total}\n' http://127.0.0.1:3000/api/health` returned HTTP 200.
- PASS: `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 ENABLE_LOCAL_ADMIN_SHORTCUT=true PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_E2E_PROFILE=core npx playwright test tests/admin-load-test-dashboard.spec.ts --project=chromium-authenticated --workers=1` - 2/2 tests passed.

## CI Promotion Status
- Feature branch PR to `dev`: pending at report creation time.
- `dev` CI after merge: pending at report creation time.
- `main` promotion and CI: pending at report creation time.

## Risks And Yellow Flags
- Removing `ContentJson` from public Work list means thumbnail discovery from inline body images is intentionally not available on list responses. This is the correct tradeoff for list DTO weight, but content relying only on inline images should use explicit thumbnail assets or video preview fallback.
- Existing `AWSSDK.Core` NU1901 warnings remain unrelated to this correction.
- The old 2026-05-05 k6 numbers are explicitly invalid for realistic workload interpretation because they used seed/pageSize=1 targets.

## Recommendation
Use the corrected Real Backend Test UI target selection for future capacity checks: list endpoints at `pageSize=12`, read endpoints from current public content order, and no seeded fallback unless explicitly chosen for a separate deterministic smoke test.
