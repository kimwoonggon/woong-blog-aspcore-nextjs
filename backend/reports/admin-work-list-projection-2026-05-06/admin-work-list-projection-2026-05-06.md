# Admin Work List Projection Audit - 2026-05-06

## Summary
- Changed `WorkQueryStore.GetAdminListAsync` from a full entity load plus Asset and WorkVideo lookups into a single EF projection matching `AdminWorkListItemDto`.
- Admin Work list now reads stored `PublicThumbnailUrl` directly, matching the write-time public media read model already used by public list/detail paths.
- Added a Postgres contract test proving the admin Work list returns the stored thumbnail URL and completes in one measured Postgres command.

## Intentionally Not Changed
- No cache was introduced.
- No Real Backend Test target selection was changed.
- No seeded slug priority was introduced.
- No public list `pageSize=12` behavior was reduced or altered.
- No public DTO shape or endpoint contract was changed.
- No admin detail behavior was changed; admin detail still loads full content and videos because the editor needs them.

## Goal Verification
- User goal: improve backend structure so DTO reads fetch only necessary DB data.
- Result: `AdminWorkListItemDto` no longer requires loading `ContentJson`, `AllPropertiesJson`, `Assets`, or `WorkVideos` for list rendering.
- The change is a read-model correction, not a benchmark-only or load-test-method change.

## Validations
- RED: `PostgresPersistenceContractTests.AdminWorkList_UsesSinglePostgresCommand_AndStoredThumbnail` failed before implementation because admin Work list returned Asset-derived URL instead of stored `PublicThumbnailUrl`.
- GREEN: the same focused test passed after direct projection.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore` passed 10/10.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` passed 40/40.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AdminEndpointsTests|FullyQualifiedName~AdminContentEndpointsTests" --no-restore` passed 33/33.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed Component 122, Unit 56, Architecture 35, Integration 214, Contract 1 skipped.
- PASS: `git diff --check` passed.
- PASS: changed-file guard found no seed-priority, `pageSize=1`, or cache-related changes.

## Risks And Follow-Up
- Admin list now relies on `PublicThumbnailUrl` being correctly populated by write/bootstrap paths. Existing bootstrap and create/update flows already populate it.
- This slice does not directly improve the public Real Backend Test mix because admin list is not one of the k6 targets.
- Remaining higher-impact work should continue on public/admin body separation, DB timing visibility, and Docker runtime hardening for the existing `libgssapi_krb5.so.2` warning.

## Recommendation
- Promote this small read-model correction through `dev` and `main` because it is low-risk, covered by Postgres command-count evidence, and aligns with the backend structure goal.
