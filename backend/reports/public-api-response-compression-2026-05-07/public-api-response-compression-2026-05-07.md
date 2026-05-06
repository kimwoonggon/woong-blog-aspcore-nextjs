# Public API Response Compression - Audit Report

Date: 2026-05-07
Branch: `perf/public-api-response-compression-2026-05-07`
Base: `dev` at `6a18e20`
Primary goal: reduce public API detail response transfer cost without changing real target selection, `pageSize=12`, caching behavior, or public DTO semantics.

## Summary

This slice enables ASP.NET Core response compression for JSON/problem+JSON/text responses and verifies the observable public API behavior through an integration test. The optimization targets large public detail responses where network transfer and response receive time contribute to Real Backend Test client p95/p99 latency.

Expected Real Backend Test impact:

- Large Work/Study detail reads with `Accept-Encoding: gzip`: lower wire bytes and usually lower `http_req_receiving`, client p95/p99, and nginx/app response transfer time.
- Small list/read responses: smaller improvement and possible negligible CPU overhead.
- HTTP success/failure rate: should remain unchanged; this is not a DB or concurrency limit fix.
- DB latency/open latency: should remain unchanged; this slice does not reduce DB roundtrips.
- CPU/GC: CPU can rise under high RPS because gzip uses backend CPU; `CompressionLevel.Fastest` was selected for the 2 CPU shared host constraint.

## Changed

- `backend/src/WoongBlog.Api/Common/ApiServiceCollectionExtensions.cs`
  - Registers `AddResponseCompression`.
  - Enables compression for HTTPS.
  - Adds gzip provider.
  - Includes `application/json` and `application/problem+json` in compressed MIME types.
  - Configures gzip as `CompressionLevel.Fastest` to avoid high CPU compression cost.

- `backend/src/WoongBlog.Api/Program.cs`
  - Adds `app.UseResponseCompression()` after forwarded/transport security middleware and before the app elapsed timing middleware.

- `backend/tests/WoongBlog.Api.IntegrationTests/PublicEndpointsTests.cs`
  - Adds behavior-focused integration coverage for `GET /api/public/works/{slug}` with `Accept-Encoding: gzip`.
  - Verifies `Content-Encoding: gzip`, `Vary: Accept-Encoding`, decompressed JSON content, and compressed payload smaller than decompressed bytes.

- `backend/tests/WoongBlog.Api.ArchitectureTests/ArchitectureBoundaryTests.cs`
  - Updates middleware order guard for the new response compression middleware.

- `backend/reports/public-api-response-compression-2026-05-07/loadtest/k6-public-work-compression-50rps-10s.json`
  - Stores the local backend-container k6 smoke result.

## Intentionally Not Changed

- No cache was introduced. This is transport compression only.
- Real Backend Test target selection was not weakened. No seeded-target forcing was added.
- Public list `pageSize=12` behavior was not changed.
- Public Work/Blog DTO field semantics were not changed except for HTTP transfer encoding.
- DB query shapes, indexes, Npgsql pool configuration, and EF projections were not changed in this slice.
- nginx timing extraction was not changed.

## Goal Verification

- Same public API endpoint still returns JSON content that decompresses to the expected public body.
- Compression only applies when the client advertises support through `Accept-Encoding: gzip`.
- `Vary: Accept-Encoding` is present, so downstream HTTP caches/proxies can distinguish compressed and uncompressed variants if they exist.
- The selected compression level is appropriate for a 2 CPU / 8 GiB shared production host because it favors low CPU over maximum byte reduction.

## Validations

- RED test before implementation:
  - `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~GetWorkBySlug_CompressesPublicDetailJson_WhenClientAcceptsGzip" --no-restore --logger "console;verbosity=minimal"`
  - Failed because `Content-Encoding: gzip` was absent.

- Focused GREEN test:
  - Same command passed 1/1 after implementation.

- Architecture middleware order:
  - `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --filter "FullyQualifiedName~Program_ComposesServicesMiddlewareAndEndpoints_InExpectedBoundaryOrder" --no-restore --logger "console;verbosity=minimal"`
  - Passed 1/1.

- Focused public endpoint integration suite:
  - `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests" --no-restore --logger "console;verbosity=minimal"`
  - Passed 31/31.

- Full backend solution:
  - `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"`
  - Passed: Contract 1 skipped, Component 124, Unit 59, Architecture 35, Integration 229.

- Dev compose rebuild:
  - `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d --build backend nginx`
  - Succeeded for backend/nginx on the current branch.

- Curl smoke:
  - Plain `GET /api/public/works/seeded-work`: HTTP 200, 1195 B.
  - Gzip `GET /api/public/works/seeded-work` with `Accept-Encoding: gzip`: HTTP 200, 850 B.
  - Response headers included `Content-Encoding: gzip`, `Vary: Accept-Encoding`, and `X-App-Elapsed-Ms`.

- k6 smoke from backend container:
  - Target: `/api/public/works/seeded-work`
  - Load: 50 rps, 10 s, max VUs 100.
  - Requests: 500.
  - Check rate: 1.0.
  - HTTP failure rate: 0.
  - `http_req_duration p95`: 2.415107 ms.
  - `http_req_receiving p95`: 0.170821 ms.
  - `data_received`: 796942 bytes.
  - Each request verified status 200 and `Content-Encoding: gzip`.

## Risks And Yellow Flags

- Compression spends CPU. On the real 2 CPU host shared by backend, frontend, nginx, and DB, a high-RPS spike can trade lower transfer time for higher CPU pressure.
- The local k6 smoke used `seeded-work`, which is intentionally small. It proves behavior and safety, not production-sized heavy detail improvement.
- The current Real Backend Test byte metric may not fully represent compressed wire bytes if the response is chunked and no `Content-Length` exists. Use k6 `data_received`, `http_req_receiving`, client p95/p99, and CPU/GC together for production-like A/B comparison.
- This slice does not address remaining DB pressure, query latency, app elapsed spikes, ThreadPool queue spikes, or 5xx under high max VU pressure.

## Final Recommendation

Merge this slice if CI is green because it is behavior-covered and low-risk. Then run a production-like A/B on real heavy Work/Study detail URLs over HTTPS:

1. Baseline without gzip or before deploy.
2. Same scenario with `Accept-Encoding: gzip` after deploy.
3. Compare client p95/p99, `http_req_receiving`, `data_received`, app elapsed p95, CPU, GC, ThreadPool queue, DB latency/open latency, and HTTP 5xx.

If CPU rises materially or p95 does not improve on heavy real details, the next better target is not more compression. It is payload shape reduction and serialization/wire-byte observability for public detail DTOs.
