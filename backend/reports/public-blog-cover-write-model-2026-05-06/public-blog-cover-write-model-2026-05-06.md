# Public Blog Cover Write Model - 2026-05-06

## Goal
Persist Blog representative cover metadata at write time so public Blog reads use stored columns instead of resolving media from mutable admin/body data at request time.

This slice keeps the corrected realistic load-test semantics:
- Work list: `/api/public/works?page=1&pageSize=12`
- Study list: `/api/public/blogs?page=1&pageSize=12`
- Work read: current public list-selected `/api/public/works/internal-admin-workbench`
- Study read: current public list-selected `/api/public/blogs/engineering-notes-on-bff-auth`
- No seeded target priority, no `pageSize=1`, and no cache masking.

## Changed
- Added `coverAssetId` to Blog create requests and commands.
- Added omitted-vs-explicit-null semantics to Blog update requests so old update callers preserve the stored cover, explicit `null` clears it, and a provided ID changes it.
- Added `IBlogCommandStore.GetAssetPublicUrlsAsync` and an EF Core `Assets.AsNoTracking()` projection for write-time public URL lookup.
- Blog create/update handlers now write `Blog.CoverAssetId` and `Blog.PublicCoverUrl` from `Assets.PublicUrl` at save time.
- Added admin integration coverage for create, preserve-on-omit, change, and clear cover behavior.
- Added a Postgres persistence contract proving public Blog detail returns stored `PublicCoverUrl` with one measured Postgres command.
- Cleaned duplicate test upload helper definitions after the RED/GREEN implementation.

## Intentionally Not Changed
- No cache was added.
- Real Backend Test target selection was not changed.
- Public list targets were not reduced; `pageSize=12` remains the realistic list target.
- No seeded Work/Blog slug priority was added.
- No public/admin body model split was implemented in this slice.
- No JSON source-generation or serializer pipeline change was implemented in this slice.
- No Docker runtime package changes were made for the pre-existing `libgssapi_krb5.so.2` startup log.

## Goal Verification
- Blog representative cover resolution is now a write-time operation for admin create/update.
- Public Blog read paths continue to project stored `PublicCoverUrl`.
- Public Blog detail has contract coverage for one measured Postgres command.
- Existing update callers that omit `coverAssetId` do not accidentally clear a cover.
- Explicit `coverAssetId: null` remains available to clear a cover intentionally.
- Load-test target semantics were preserved: current public read slugs and `pageSize=12` lists.

## Validation
- RED: `CreateAndReadBlog_PersistsUploadedCover` failed before implementation because public Blog detail did not include the uploaded cover URL.
- GREEN: the same create-cover test passed after Blog create writes `CoverAssetId` and `PublicCoverUrl`.
- RED: `UpdateBlog_PreservesStoredCover_WhenCoverAssetIdIsOmitted` failed after the first implementation because legacy update requests cleared the stored cover.
- GREEN: update now distinguishes omitted `coverAssetId` from explicit null; create, preserve, change, and clear tests passed.
- PASS: `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter "FullyQualifiedName~RequestValidatorTests" --no-restore` - 8/8.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AdminContentEndpointsTests" --no-restore` - 25/25.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PostgresPersistenceContractTests" --no-restore` - 9/9.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests" --no-restore` - 29/29.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` - 40/40.
- PASS: `git diff --check` completed with no whitespace errors.
- PASS: diff guard found no production load-target, seed-priority, or `pageSize` change.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` - Component 122/122, Unit 56/56, Architecture 35/35, Integration 213/213, Contract 1 skipped due no pact files.
- PASS: Docker compose rebuilt backend/frontend under the 2 CPU / about 7.5 GiB total overlay and `/api/health` returned OK.
- PASS: current public target probes selected Work read `/api/public/works/internal-admin-workbench` and Study read `/api/public/blogs/engineering-notes-on-bff-auth` from public list order.

Existing warning observed in .NET validations:
- `AWSSDK.Core` NU1901 low-severity advisory remains pre-existing and unrelated to this work.

## Load Test Result
2 CPU shared app stack, about 7.5 GiB aggregate container memory:
- backend: cpuset `0,1`, 3 GiB
- frontend: cpuset `0,1`, 1 GiB
- db: cpuset `0,1`, 3 GiB
- nginx: cpuset `0,1`, 512 MiB
- k6 runner: Docker container pinned to CPU 2 so it did not consume the simulated 2 CPU server budget

Constant public mix, 300 rps, 30s, current public read targets, `pageSize=12`:
- Requests: 9,001
- Throughput: 300.01 rps
- Failed rate: 0
- Dropped iterations: 0
- Aggregate p50: 1.58 ms
- Aggregate p95: 4.10 ms
- Max: 98.18 ms
- Work list p95: 12.21 ms
- Study list p95: 3.49 ms
- Work read p95: 3.13 ms
- Study read p95: 2.67 ms

Artifact:
- `backend/reports/public-blog-cover-write-model-2026-05-06/loadtest/k6-2cpu-total8gb-current-public-mix-300rps-30s.json`

## Risks And Yellow Flags
- This slice fixes the Blog cover write model and protects public Blog read shape, but it is not the body payload split; large public detail payload serialization can still become the next bottleneck.
- The local k6 run is a 300 rps regression/capacity sanity check, not proof that the 1000 rps spike target is achieved.
- The local prod-like database uses the current local public list-selected slugs, not the exact production content corpus.
- Backend container startup logs still include a pre-existing `libgssapi_krb5.so.2` missing message on Alpine. The app served the k6 run successfully, but the runtime image should be cleaned up in a separate Docker hardening slice.
- MediatR license startup warning is still present in this production-environment local compose run and should be classified separately from request-path performance.

## Recommendation
Ship this slice because it closes a real write-model gap for Blog representative covers without manipulating load-test targets or adding cache. Continue with the next performance slice focused on public/admin body model split, app serialization timing, DB command/open p95 availability, and k6 p99 export before considering cache.
