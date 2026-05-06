# Public Home Shell Query Audit - 2026-05-06

## Goal
Reduce DB roundtrip and pool pressure on the public Home read path without cache, seeded-target shortcuts, or public list `pageSize` manipulation.

This slice focuses on `/api/public/home`, which still performed sequential reads for home page, site settings, resume asset, featured works, and recent posts after the Work/Blog list/detail optimizations.

## Changed
- Added `HomeShellDto` to represent the public Home page metadata plus site settings summary.
- Added `IHomeQueryStore.GetHomeShellAsync`.
- Changed `GetHomeQueryHandler` to load the Home shell first, then load featured works and recent posts.
- Added a `HomeQueryStore.GetHomeShellAsync` EF projection that combines:
  - `Pages` row for slug `home`
  - singleton `SiteSettings`
  - optional resume `Assets.PublicUrl`
- Added a Postgres contract test proving a complete public Home request now executes 3 measured DB commands instead of the previous 5.
- Captured a scoped k6 public Home smoke result under the existing local 2 CPU / shared 8 GiB compose overlay.

## Intentionally Not Changed
- No cache was added.
- Real Backend Test target selection was not changed.
- No seeded Work/Blog target priority was added.
- No public list target was reduced to `pageSize=1`; existing `pageSize=12` semantics remain unchanged.
- Public Home DTO shape remains `homePage`, `siteSettings`, `featuredWorks`, and `recentPosts`.
- Public Work/Blog detail DTO shape and list/query semantics were not changed in this slice.
- Existing `GetHomePageAsync` and `GetSiteSettingsSummaryAsync` methods were left available to keep the store interface backward-compatible for current callers/tests.

## Verification Against Goals
- Before implementation, the new Postgres test failed with expected 3 commands vs actual 5 commands.
- After implementation, the same public Home behavior returns the same DTO data while command count drops to 3:
  - one command for Home shell plus resume URL
  - one command for featured Work cards
  - one command for recent Blog cards
- The change is a query-shape/read-model improvement, not a cache workaround.
- The changed source/test files contain no seed-priority target logic, `pageSize=1` shortcut, or cache usage.

## Validations Performed
- RED: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests.PublicHome_UsesThreePostgresCommands_ForShellAndSummaryProjections" --no-restore --logger "console;verbosity=minimal"` failed: expected 3 DB commands, actual 5.
- GREEN: same focused test passed after Home shell projection implementation.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore --logger "console;verbosity=minimal"` passed 40/40.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore --logger "console;verbosity=minimal"` passed 14/14.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests|FullyQualifiedName~StartupCompositionTests" --no-restore --logger "console;verbosity=minimal"` passed 42/42.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed backend solution: Component 122/122, Unit 56/56, Architecture 35/35, Integration 223/223, Contract 1 skipped because no Pact files were present.
- PASS: `git diff --check` completed with no whitespace errors.
- PASS: source/test diff guard found no `pageSize=1`, seeded target priority, or cache-related production/test changes in this slice.
- PASS: rebuilt backend under the existing 2 CPU / shared 8 GiB compose overlay and verified `http://127.0.0.1:3100/api/health` returned 200.
- PASS: k6 public Home smoke `RATE=100 DURATION_SECONDS=15 MAX_VUS=200 TARGET_PATH=/api/public/home` via Docker network completed 1,500 requests at 99.99 rps, failed rate 0, p95 25.44 ms, max 581.38 ms.
- PASS: k6 summary JSON parsed successfully with Node and verified 1,500 requests and failed rate 0.

## Load Test Artifact
- `backend/reports/public-home-shell-query-2026-05-06/loadtest/k6-public-home-100rps-15s.json`

## Risks And Yellow Flags
- This improves `/api/public/home`; it does not directly change the four Real Backend public-api-mix URLs unless Home is added to a future scenario.
- Public Home still returns `PageSummaryDto.ContentJson`; if Home page content grows large, Page public/admin body splitting should be handled as a separate data-model slice.
- Public Home still uses three commands. Further collapse would require combining heterogeneous collections or denormalizing Home cards, which is not justified until Home is a measured bottleneck.
- The k6 smoke is a regression sanity check, not a 1000 rps capacity claim.
- Existing `AWSSDK.Core` NU1901 low-severity warnings remain unrelated to this slice.

## Final Recommendation
Ship this slice through `dev` because it removes two avoidable DB roundtrips from a public endpoint while preserving API behavior and realistic load-test semantics. Next improvement should return to the measured hot set: either add persistent Real Backend diagnostics snapshots to completed run artifacts or profile JSON serialization/payload cost for heavy detail reads before attempting source-generated JSON changes.
