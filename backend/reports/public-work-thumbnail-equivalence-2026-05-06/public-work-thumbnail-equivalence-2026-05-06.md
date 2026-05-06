# Public Work Thumbnail Equivalence Guard - 2026-05-06

## Summary
Added Postgres contract coverage for the stored public Work thumbnail read model. The new tests verify that, for explicit thumbnail assets, the value previously produced by `WorkThumbnailUrlResolver.ResolveThumbnailUrl(...)` is exactly the value returned by the public Work list/detail projections that read `PublicThumbnailUrl`.

## Changed
- Extended `PostgresPersistenceContractTests.PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand_AndResolverEquivalentStoredThumbnail` to assert resolver-equivalent thumbnail output on public Work detail while preserving the one-Postgres-command contract.
- Extended `PostgresPersistenceContractTests.PublicWorkFirstPage_UsesSinglePostgresCommand_ForNoSearchList` to assert resolver-equivalent thumbnail output on the public Work first-page list while preserving the one-Postgres-command contract.
- Kept the existing admin Work list resolver-equivalence guard as coverage for the admin projection path.
- Updated `todolist-2026-05-06.md` with plan alignment and validation results.

## Intentionally Not Changed
- No production source code was changed.
- No cache was introduced.
- No Real Backend Test target selection was changed.
- No seeded slug priority was introduced.
- No public list `pageSize=12` behavior was reduced or replaced.
- No body-image or video thumbnail fallback was reintroduced on public hot paths; those fallbacks remain intentionally excluded for performance.

## Goal Verification
- Goal: guarantee that replacing resolver-time thumbnail computation with stored `PublicThumbnailUrl` is safe where equivalence is expected.
- Result: explicit thumbnail asset paths are now covered for public detail, public list, and admin list.
- Boundary: equivalence is intentionally asserted only for explicit thumbnail assets. Historical resolver fallback to body images or videos is not treated as required equivalence for public hot paths because it was removed to avoid request-time body/video scanning.

## Validations
- PASS: focused resolver-equivalence guard filter passed 3/3.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore` passed 10/10.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed backend tests: Component 122/122, Unit 56/56, Architecture 35/35, Integration 214/214, Contract 1 skipped due no pact files.
- PASS: `git diff --check` completed with no whitespace errors.

## Risks And Follow-Up
- The guard does not prove equivalence for removed fallback behavior. If product requirements still demand video/body fallback thumbnails publicly, that must be reintroduced as a write-time/backfill model, not as request-time parsing.
- This slice is test-only; it prevents regression but does not improve runtime performance by itself.
- Existing AWSSDK.Core NU1901 warnings remain unrelated to this change.

## Recommendation
Merge this guard before continuing the next backend performance slice. Then continue with DB roundtrip/timing instrumentation or JSON serialization allocation reduction.
