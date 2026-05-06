# Admin Work List Projection Audit - 2026-05-06

## Summary
- Changed `WorkQueryStore.GetAdminListAsync` from a full entity load plus Asset and WorkVideo lookups into a single EF projection matching `AdminWorkListItemDto`.
- Admin Work list now reads stored `PublicThumbnailUrl` directly, matching the write-time public media read model already used by public list/detail paths.
- Added Postgres contract coverage proving the admin Work list returns the same thumbnail URL the existing resolver would return for an explicit thumbnail asset and completes in one measured Postgres command.
- Added create endpoint coverage proving write-time `PublicThumbnailUrl` is stored as the existing resolver result for an uploaded thumbnail asset.

## Intentionally Not Changed
- No cache was introduced.
- No Real Backend Test target selection was changed.
- No seeded slug priority was introduced.
- No public list `pageSize=12` behavior was reduced or altered.
- No public DTO shape or endpoint contract was changed.
- No admin detail behavior was changed; admin detail still loads full content and videos because the editor needs them.
- Public Work card/detail policy remains unchanged: video/body fallback is not reintroduced for public thumbnails.

## Goal Verification
- User goal: improve backend structure so DTO reads fetch only necessary DB data.
- Result: `AdminWorkListItemDto` no longer requires loading `ContentJson`, `AllPropertiesJson`, `Assets`, or `WorkVideos` for list rendering.
- Correctness guard: tests now verify the stored URL equals `WorkThumbnailUrlResolver.ResolveThumbnailUrl(...)` for the explicit-thumbnail case that the list projection relies on.
- The change is a read-model correction, not a benchmark-only or load-test-method change.

## Validations
- RED: `PostgresPersistenceContractTests.AdminWorkList_UsesSinglePostgresCommand_AndStoredThumbnail` failed before implementation because admin Work list returned Asset-derived URL while the first projection test intentionally set a divergent stored URL.
- CORRECTION: that first test was too weak. It was updated to assert resolver result equality instead of merely stored-field precedence.
- GREEN: focused resolver-equivalence projection test passed after implementation.
- PASS: `AdminContentEndpointsTests.CreateAndReadWork_PersistsUploadedThumbnailAndIcon` now asserts write-time stored `PublicThumbnailUrl` equals the resolver result for the uploaded asset.
- PASS: focused equivalence filter passed 2/2.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore` passed 10/10 before the correction, and focused equivalence tests passed after the correction.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` passed 40/40.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AdminEndpointsTests|FullyQualifiedName~AdminContentEndpointsTests" --no-restore` passed 33/33 before the correction, and the changed focused admin content test passed after the correction.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed Component 122, Unit 56, Architecture 35, Integration 214, Contract 1 skipped after the equivalence correction.
- PASS: `git diff --check` passed before audit update.
- PASS: changed-file guard found no seed-priority, `pageSize=1`, or cache-related changes.

## Risks And Follow-Up
- Admin list now relies on `PublicThumbnailUrl` being correctly populated by write/bootstrap paths. Explicit thumbnail asset create/update is covered; direct out-of-band DB writes can still create stale stored URLs.
- This slice does not directly improve the public Real Backend Test mix because admin list is not one of the k6 targets.
- Remaining higher-impact work should continue on public/admin body separation, DB timing visibility, and Docker runtime hardening for the existing `libgssapi_krb5.so.2` warning.

## Recommendation
- Promote this small read-model correction through `dev` and `main` after the corrected equivalence guard is pushed and CI is green.
