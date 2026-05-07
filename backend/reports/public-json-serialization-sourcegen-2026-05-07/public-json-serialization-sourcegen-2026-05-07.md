# Public JSON Serialization Source Generation Audit - 2026-05-07

## Summary
- Enabled System.Text.Json source-generated serialization mode for the API JSON context used by public hot-path response DTOs.
- Added startup composition regression coverage so both Minimal API `HttpJsonOptions` and MVC JSON options must keep `Metadata` mode and enable `Serialization` mode.
- Preserved the existing public API DTO set, camelCase naming policy, response compression, query projections, and database read model behavior.

## Changed Files
- `backend/src/WoongBlog.Api/Common/Json/WoongBlogApiJsonSerializerContext.cs`
  - Changed generation mode from `Metadata` to `Metadata | Serialization`.
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupCompositionTests.cs`
  - Added `JsonOptions_UseSourceGeneratedSerializationForPublicHotPathDtos`.
  - Added reflection-based assertion against `JsonSourceGenerationOptionsAttribute.GenerationMode`.
- `todolist-2026-05-07.md`
  - Recorded this TDD slice, validations, and local Docker/Testcontainers limitation.

## Intentionally Not Changed
- No public API response shape changes.
- No seeded target substitution, no `pageSize` reduction, and no load-test target realism changes.
- No caching or cache headers added.
- No database schema, EF projection, Npgsql pool, nginx, or Docker configuration changes.
- No endpoint-level explicit `JsonTypeInfo<T>` rewrite; this slice keeps the existing centralized JSON options path.

## Goal Verification
- JSON serialization allocation reduction: partially advanced. The registered public hot-path DTOs now request generated serialization code instead of metadata-only source generation.
- Public Work/Blog response compatibility: verified by existing public endpoint tests and frontend public API parser/contract tests.
- DB pressure/roundtrip goals: unchanged by design. This slice should not affect DB command count, DB latency, or pool behavior.
- Observability goals: unchanged by design. Existing Real Backend Test app/nginx/db timing should measure any effect.
- HLS upload failure: out of scope for this slice; prior nginx API timeout and upload limit fixes remain separate.

## Expected Real Backend Test Impact
- `ASP.NET app elapsed p95`: should improve modestly when JSON serialization is a meaningful part of public detail request time.
- `GC heap`, `Gen2 GC`, `Time in GC`: should trend down under detail-heavy load due to less serializer runtime work/allocation.
- `client p95/p99`: may improve if app elapsed was the bottleneck; network payload size is unchanged, so this is not a bandwidth fix.
- `throughput`: possible small improvement; not expected to resolve saturation caused by DB pool, CPU quota, or oversized content.
- `DB command P95`, `DB connection open P95`, `Open connections`: no expected direct change.

## Validations
- RED: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~JsonOptions_UseSourceGeneratedSerializationForPublicHotPathDtos" --no-restore --logger "console;verbosity=minimal"`
  - Failed before implementation because `JsonSourceGenerationMode.Serialization` was not enabled.
- GREEN: same focused startup/config test passed 1/1 after implementation.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~JsonOptions_UseSourceGenerated|FullyQualifiedName~PublicEndpointsTests" --no-restore --logger "console;verbosity=minimal"`
  - Passed 33/33.
- WARN: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"`
  - Failed only because local Docker/Testcontainers is unavailable for `PostgresPersistenceContractTests`.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --filter "FullyQualifiedName!~PostgresPersistenceContractTests" --logger "console;verbosity=minimal"`
  - Passed: Contract 1 skipped, Component 126, Unit 59, Architecture 39, Integration 215.
- PASS: `npm test -- --run src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts`
  - Passed 22/22.
- PASS: `npm run typecheck`
- PASS: `npm run lint`
  - 0 errors, 7 existing warnings.
- PASS: `git diff --check`
- WARN: `docker --version`
  - Docker is not available in this WSL runtime, so local compose, Testcontainers PostgreSQL, and k6/real backend load smoke were not run here.

## Risks And Yellow Flags
- The improvement is structural but not a direct benchmark result. The actual Real Backend Test delta must be measured on the 2 CPU / total 8 GiB production-like runtime.
- Source-generated serialization may not dominate latency if heavy content size, compression, CPU quota, or DB pool pressure dominates the run.
- CI must run Docker-backed PostgreSQL contract tests because local Docker is unavailable in this environment.
- Global `Metadata | Serialization` applies to the registered API JSON context types. Focused tests passed, but CI remains the stronger compatibility gate.

## Final Recommendation
- Ship this slice through `dev` CI. If CI passes, promote to `main` and confirm GHCR publish.
- After deployment, rerun the same real backend profile used for the current baseline and compare `ASP.NET app elapsed p95`, `GC heap`, `Gen2 GC`, `Time in GC`, and client p95/p99 before choosing the next backend performance slice.
