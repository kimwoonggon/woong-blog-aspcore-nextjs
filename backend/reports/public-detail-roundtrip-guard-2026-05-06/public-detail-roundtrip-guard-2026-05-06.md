# Public Detail Roundtrip Guard - 2026-05-06

## Scope
Added a Postgres persistence contract that protects the existing public Work detail read-path optimization for heavy detail pages with videos.

## Changed
- Added `PublicWorkDetailWithVideos_UsesTwoPostgresCommands_AndStoredPublicColumnsOnly` in `PostgresPersistenceContractTests`.
- The test verifies a published Work with videos returns public content and representative thumbnail from stored public columns.
- The test verifies exactly 2 Postgres reader commands: one Work detail projection and one Work video projection.
- The test verifies the combined SQL does not select `ContentJson`, `AllPropertiesJson`, or `Assets` for the public detail read path.
- Updated `todolist-2026-05-06.md` with the slice plan and validation log.

## Intentionally Not Changed
- No production code was changed in this slice.
- No cache layer was introduced.
- No Real Backend target selection logic was changed.
- No seed-priority fallback was introduced.
- No `pageSize=12` behavior was reduced or replaced.
- No public API DTO shape was changed.

## Goal Verification
- Backend structure: verified public Work detail with videos uses bounded DB work and does not read original body/admin JSON columns on the request path.
- Stored representative media: verified `PublicThumbnailUrl` remains resolver-equivalent for the explicit thumbnail case.
- Public/admin split: verified public detail reads stored public body fields rather than selecting admin/original JSON columns.
- Backend load-test impact: this is a regression guard, not a direct latency-lowering patch. It protects the changes that should keep heavy Work detail requests from reintroducing extra query/JSON work.

## Validation
- RED: focused test initially failed because the fixture's manually assigned `PublicContentHtml` was recomputed from `ContentJson` by the SaveChanges synchronizer; the fixture did not yet isolate read-path stored-column behavior.
- GREEN: focused test passed after the fixture updated stored public fields post-save with raw SQL.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicWorkDetailWithVideos_UsesTwoPostgresCommands_AndStoredPublicColumnsOnly" --no-restore --logger "console;verbosity=minimal"`.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore --logger "console;verbosity=minimal"` passed 15/15.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed: Contract 1 skipped, Component 124, Unit 56, Architecture 35, Integration 226.
- PASS: `git diff --check`.
- PASS: diff guard found no added runtime `pageSize=1`, seed-priority, or cache workaround; the only matches were TODO wording describing non-goals.

## Risks And Yellow Flags
- The new test intentionally asserts SQL column/command shape because the requirement is performance-contract oriented. If a future query strategy changes while preserving the same performance envelope, the test may need a deliberate update.
- This slice does not produce a new Real Backend p95 number because no runtime code changed.
- Work public detail with videos still needs 2 commands by design: one for Work detail and one for videos. That is within the target of 1-2 DB roundtrips.
- Per-target DB command attribution is still unavailable in the UI; current DB metrics are run-level diagnostics.

## Recommendation
Merge this guard through CI, then choose the next actual production optimization from live 2CPU/8GiB evidence. The next useful target is not another test-method change; it should be either per-target DB/app timing attribution or a measured production-code reduction in serialization/payload/EF work if the next Real Backend run shows app elapsed or DB-open latency as the limiter.
