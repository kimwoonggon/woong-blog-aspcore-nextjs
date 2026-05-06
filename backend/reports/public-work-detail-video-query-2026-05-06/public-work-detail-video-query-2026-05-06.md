# Public Work Detail Video Query Audit - 2026-05-06

## Scope

This slice improves the backend read path for `GET /api/public/works/{slug}` without changing Real Backend Test scenarios, target selection, list page size, cache policy, or response contract.

## Goals

- Avoid an unnecessary `WorkVideos` database command when a published public Work detail has no videos.
- Keep public Work detail behavior unchanged for clients: return the same `WorkDetailDto` shape with `videos` as an empty list when no videos exist.
- For Work details that do have videos, project only fields needed to build `WorkVideoDto` instead of materializing full `WorkVideo` entities on the public path.
- Preserve realistic load-test semantics: no seeded-target priority, no `pageSize=12` reduction, no cache-first fix.
- Validate under the existing local 2 CPU cpuset / shared 8 GiB compose overlay.

## Changed

- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkQueryStore.cs`
  - Public Work detail projection now includes `HasVideos` via an `EXISTS` check in the primary Work query.
  - Public detail construction skips the `WorkVideos` query entirely when `HasVideos` is false.
  - Public video read path now projects into `PublicWorkVideoRow` with only public DTO fields and then maps to `WorkVideoDto`.
  - Admin detail path still uses the existing full video entity load and preview asset verification behavior.
  - `ResolvePreviewStorageType` now accepts `sourceType` and `sourceKey`, allowing both entity and projected-row paths to share the resolver.

- `backend/tests/WoongBlog.Api.IntegrationTests/PostgresPersistenceContractTests.cs`
  - Added `PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand`.
  - RED failure before implementation was `Expected: 1 Actual: 2`, proving the needless no-video `WorkVideos` query existed.
  - GREEN verifies a published Work with no videos returns empty `Videos` and executes exactly one PostgreSQL command.

- `todolist-2026-05-06.md`
  - Added the dated TODO section, user instruction mapping, validation progress, and promotion follow-up status for this slice.

- `backend/reports/public-work-detail-video-query-2026-05-06/loadtest/`
  - Added k6 run output and parsed summary for the scoped local sanity run.

## Intentionally Not Changed

- Did not add or rely on cache.
- Did not change Real Backend Test scenario definitions.
- Did not prioritize seeded Work/Blog targets.
- Did not reduce `pageSize=12` to a smaller value.
- Did not remove public body content from Work detail.
- Did not alter Blog detail/read paths in this slice.
- Did not change DB schema, indexes, connection pool sizing, nginx worker settings, or Docker resource limits.
- Did not modify admin Work video behavior or preview asset verification.

## Goal Verification

- No-video public Work detail now uses one DB command instead of two.
- Work details with videos still return videos, but the public path fetches only public video DTO fields.
- List target remains `pageSize=12` in the load sanity run.
- No seed-priority logic or cache logic was introduced.
- The local sanity run used the current visible public Work `/api/public/works/internal-admin-workbench`, which had `videos=0`; this exercises the optimized path directly.

## Validations

- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicWorkDetailWithoutVideos_UsesSinglePostgresCommand" --no-restore`
  - 1 passed, 0 failed.

- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore`
  - 40 passed, 0 failed.

- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore`
  - 8 passed, 0 failed.

- PASS: `dotnet test backend/WoongBlog.sln --no-restore`
  - Component: 122 passed.
  - Unit: 56 passed.
  - Architecture: 35 passed.
  - Integration: 209 passed.
  - Contract: 1 skipped.

- PASS: `git diff --check`
  - No whitespace errors.

- PASS: Backend Docker image rebuilt under the existing 2 CPU cpuset / shared 8 GiB overlay.
  - Startup environment verified: `ASPNETCORE_ENVIRONMENT=Production`, `LoadTesting__BaseUrl=http://nginx`, Postgres max pool size 40.
  - Health checks: backend direct 200, nginx HTTP 200.

- PASS: k6 local sanity under the 2 CPU overlay.
  - Scenario: `constant`, 300 rps, 30 seconds, pre-allocated VUs 300, max VUs 300.
  - Targets: Work list pageSize 12, Study list pageSize 12, Work read `/api/public/works/internal-admin-workbench`.
  - Requests: 9,001.
  - RPS: 300.05.
  - Failed rate: 0.
  - Dropped iterations: 0.
  - Aggregate p95: 7.86 ms.
  - Work read p95: 5.96 ms.
  - Study list p95: 80.44 ms.
  - Summary: `backend/reports/public-work-detail-video-query-2026-05-06/loadtest/k6-work-detail-no-video-mix-300rps-30s.summary.json`.

- PASS: Previous main promotion follow-up was also checked.
  - `Publish GHCR Main` run `25414207107` completed successfully for main head `0bf6c585d7be641ebc11944b2f083ec0016103a5`.

## Risks And Yellow Flags

- The no-video optimization adds an `EXISTS` expression to the primary Work detail query. This is cheaper than a second round trip for no-video rows, but still should be watched on a large `WorkVideos` table.
- Work details with videos still require a second query. This is intentional because videos are a collection; collapsing everything into one join risks row multiplication and larger payloads.
- This slice optimizes only public Work detail videos. Blog detail/body size, Work/Blog detail body projection, and DB index tuning remain separate follow-ups.
- Local k6 sanity is not a production capacity claim. It confirms the optimized path under the local 2 CPU overlay, not the final external HTTPS Real Backend Test ceiling.
- Backend startup logs still include existing `libgssapi_krb5.so.2` missing and MediatR license warnings. No request errors appeared during the sanity run.
- The first Docker restart attempt failed because `loadtest.env` was omitted and compose tried to publish backend on default `8080`; rerun with explicit `--env-file` succeeded.

## Final Recommendation

Proceed with PR to `dev`. After CI is green, merge to `dev`, verify auto-promotion to `main`, then run the external Real Backend Test again with realistic public Work/Study targets and `pageSize=12` unchanged.
