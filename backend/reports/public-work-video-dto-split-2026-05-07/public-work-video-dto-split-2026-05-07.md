# Public Work Video DTO Split - 2026-05-07

## Scope
- Objective area: heavy public Work detail read payload and JSON serialization reduction.
- This slice separates public Work video DTO shape from the admin/mutation `WorkVideoDto` shape.
- The change does not alter Real Backend Test targets, page size, DB roundtrip shape, or cache behavior.

## Goals
- Public Work detail should not expose admin/management-only video fields.
- Public Work detail should keep only fields used by public rendering: id, source type/key, playback URL, MIME type, dimensions, duration, timeline preview URLs, and sort order.
- Admin/mutation endpoints should keep `WorkVideoDto` with original file name, file size, and created timestamp.
- `PublicVideosJson` should be written/backfilled as a public read model, not an admin video snapshot.

## Changes Made
- Added `PublicWorkVideoDto` and changed `WorkDetailDto.Videos` to use it.
- Kept `WorkVideoDto` unchanged for admin/mutation payloads.
- Changed public Work detail mapping to emit `PublicWorkVideoDto`.
- Changed `WorkPublicVideosReadModel.Serialize` and `WorkPublicVideoSnapshot` to omit `originalFileName`, `fileSize`, and `createdAt` from newly written `PublicVideosJson`.
- Updated bootstrap SQL for new databases and added a new one-time schema patch `20260507_public_work_videos_public_dto` for existing databases whose earlier public video read-model patch was already marked as applied.
- Updated backend endpoint/component/Postgres contract tests to assert the public payload/read model omits admin-only video fields.
- Updated `WoongBlogApiJsonSerializerContext` to include `PublicWorkVideoDto`.

## RED Evidence
- Focused public endpoint test failed before implementation:
  - `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~GetWorkBySlug_ReturnsSerializedDetailWithMedia_ForAnonymousClient" --no-restore --logger "console;verbosity=minimal"`
  - Result: failed because public Work video JSON still emitted an admin-only field.

## Validation Performed
- Focused RED test after implementation:
  - Result: passed 1/1.
- Focused integration subset for public endpoint and WorkVideo endpoint plus Postgres contracts:
  - Result: public endpoint/WorkVideo endpoint tests passed 3/3.
  - Local Postgres contract tests failed before execution because `docker` is unavailable in this WSL distro, so Testcontainers could not create the PostgreSQL fixture.
- Component tests:
  - `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~GetWorkBySlugQueryHandler_ReturnsPublicDetailFromStoredFields|FullyQualifiedName~WorkVideo" --no-restore --logger "console;verbosity=minimal"`
  - Result: passed 28/28.
- Frontend public API parser/contract tests:
  - `npm test -- --run src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts`
  - Result: passed 22/22.
- Backend solution tests excluding Docker-required Postgres contract suite:
  - `dotnet test backend/WoongBlog.sln --no-restore --filter "FullyQualifiedName!~PostgresPersistenceContractTests" --logger "console;verbosity=minimal"`
  - Result: Contract 1 skipped, Component 126 passed, Unit 59 passed, Architecture 35 passed, Integration 214 passed.
- TypeScript:
  - `npm run typecheck`
  - Result: passed.
- Static diff check:
  - `git diff --check`
  - Result: passed.

## Local Blockers
- `docker` is not available in this WSL distro, so local Testcontainers/Postgres contracts and compose-based load smoke could not run.
- `k6` is not installed locally, so direct non-Docker k6 smoke could not run.
- CI must verify `PostgresPersistenceContractTests`, compose smoke, and browser smoke.

## Expected Backend Test Impact
- Work read payload bytes decrease for video-bearing Work details because `originalFileName`, `fileSize`, and `createdAt` are removed from public response JSON.
- JSON serialization work and allocation decrease modestly on Work read responses with videos.
- DB command count and DB open latency are expected to stay unchanged because Work detail already uses one query and reads videos from `Works.PublicVideosJson`.
- New writes and bootstrap backfill also shrink the stored public video read model, reducing row payload for video-bearing Work details.

## Intentionally Not Changed
- No cache was added.
- No target selection or `pageSize=12` behavior was changed.
- No public body endpoint split was introduced in this slice.
- No admin/mutation video payload fields were removed.
- No Work video playback behavior or timeline preview behavior was removed.

## Risks
- Existing clients that incorrectly relied on admin-only fields from public Work detail would stop seeing those fields. Frontend parser tests passed and the public Work page uses rendering fields that remain.
- The new bootstrap patch must run in a real PostgreSQL CI/runtime environment to strip existing `PublicVideosJson` values; local Docker absence prevented verifying that path locally.

## Recommendation
- Push this branch to `dev` and let CI verify the Docker/Postgres and compose gates.
- If CI passes, merge to `dev`, then verify automatic main promotion and GHCR publish.
