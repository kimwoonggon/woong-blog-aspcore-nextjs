# Public Content Body Single Render Source Audit

Date: 2026-05-10

## Summary
- Changed public detail body mapping so `PublicContentBodyDto.FromStoredFields` exposes only one render source.
- If `PublicContentMarkdown` exists, public DTO returns `markdown` and omits `html`.
- If markdown is missing, public DTO falls back to `html`.
- Added unit coverage for both-source and html-fallback behavior.
- Updated the Postgres public work detail contract expectation so stored duplicate body fields do not both surface in the public DTO.

## Intentionally Not Changed
- No caching was added.
- No load-test target selection was changed.
- No `pageSize=12` behavior was changed.
- No seed fixture behavior was introduced.
- No query shape change was made in this slice; public detail query stores already read stored public columns rather than request-time admin `ContentJson`.
- No frontend rendering logic was changed; existing clients already accept both `content.markdown` and `content.html` and prefer markdown.

## Goal Verification
- Goal: reduce heavy public detail payload and serialization work by returning the minimum public body form.
- Result: public detail DTO no longer returns both html and markdown when both stored columns contain values.
- Expected backend load-test impact: lower response bytes and System.Text.Json serialization/allocation work on markdown-backed detail reads. This is a targeted payload reduction, not a DB roundtrip or caching change.

## Validations
- RED: `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter "FullyQualifiedName~PublicContentBodyDtoTests" --no-restore --logger "console;verbosity=minimal"` failed before implementation because `Html` was not null when both stored sources existed.
- PASS: `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter "FullyQualifiedName~PublicContentBodyDtoTests" --no-restore --logger "console;verbosity=minimal"` passed 2/2.
- PASS: `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"` passed 61/61.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~GetWorkBySlug_ReturnsPublicContentBodyWithoutAdminContentJson|FullyQualifiedName~GetBlogBySlug_ReturnsPublicContentBodyWithoutAdminContentJson" --no-restore --logger "console;verbosity=minimal"` passed 2/2.
- BLOCKED-LOCAL: focused Postgres Testcontainers command could not start because local Docker endpoint is not running or configured.
- PASS: `npm test -- --run src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts` passed 22/22.
- PASS: `npm run typecheck` passed.
- PASS: `git diff --check -- backend/src/WoongBlog.Application/Modules/Content/Common/PublicContentBodyDto.cs backend/tests/WoongBlog.Api.UnitTests/PublicContentBodyDtoTests.cs backend/tests/WoongBlog.Api.IntegrationTests/PostgresPersistenceContractTests.cs todolist-2026-05-10.md` passed.

## Risks And Follow-Up
- The Postgres contract assertion was updated but not executed locally because Docker/Testcontainers is unavailable in this environment.
- CI should run the Postgres-backed suite with Docker available before merge.
- This slice reduces duplicate body payload only when both stored fields are present; it does not solve large markdown body size by itself.
- Next performance slice should measure public detail response bytes and consider further public body shaping or streaming/source-generated serialization if load-test evidence still points to serialization/allocation pressure.

## Recommendation
- Push this slice to a PR targeting `dev` and let CI run the Docker-backed Postgres contract tests.
- After CI passes, run real backend load tests and compare detail endpoint response bytes, p95 latency, GC heap, and Gen2 GC deltas.
