# Heavy Detail Read Optimization - 2026-05-06

## Goal
Improve the next backend bottleneck after the public list/home-card query fixes without masking the workload through cache, seed-priority target selection, or `pageSize=1` shortcuts.

The target workload remains realistic public API load:
- Work list: `/api/public/works?page=1&pageSize=12`
- Study list: `/api/public/blogs?page=1&pageSize=12`
- Work read: current public list-selected `/api/public/works/internal-admin-workbench`
- Study read: current public list-selected `/api/public/blogs/engineering-notes-on-bff-auth`

## Changed
- Public Work detail now uses an EF projection row instead of loading the full `Work` entity.
- Public Work detail resolves explicit thumbnail/icon URLs in the projected row instead of issuing a separate asset lookup roundtrip.
- Public Work detail no longer passes `ContentJson` to `WorkThumbnailUrlResolver` for public thumbnail fallback, so body inline images are not parsed as representative thumbnails at request time.
- Public Blog detail now projects directly into `BlogDetailDto` and resolves cover URL in the same query instead of loading the full `Blog` entity and then querying the asset separately.
- Added component coverage proving public Work detail keeps `ContentJson` in the response but does not use an inline body image as `ThumbnailUrl`.
- Added repo-managed `nginx/nginx.conf` with `worker_connections 4096`, `worker_rlimit_nofile 8192`, `multi_accept on`, and shorter keepalive timeout.
- Added compose-level Npgsql pool defaults: `POSTGRES_MAX_POOL_SIZE=40`, `POSTGRES_MIN_POOL_SIZE=0`, and connection strings with `Maximum Pool Size=40;Minimum Pool Size=0` by default.
- Added nginx main config mount to local/dev/staging/prod compose files.
- Updated env examples for nginx main config and DB pool sizing.

## Intentionally Not Changed
- No cache was added.
- Real Backend Test target selection was not changed to force `seeded-work` or `seeded-blog`.
- Real Backend Test list size was not reduced to `pageSize=1`; list targets remain `pageSize=12`.
- No database schema migration was added for stored representative image columns in this slice.
- Public/admin body model split was not implemented in this slice.
- JSON source-generation was not implemented in this slice.

## Validation
- RED: `GetWorkBySlugQueryHandler_DoesNotUseBodyImageAsPublicThumbnailFallback` failed before implementation because public Work detail returned `/media/detail-body-inline.png` from body JSON.
- GREEN: same focused component test passed after public Work detail stopped passing `ContentJson` to thumbnail fallback.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` - 36/36.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests" --no-restore` - 27/27.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` before config slice - Component 118/118, Unit 56/56, Architecture 35/35, Integration 202/202, Contract 1 skipped.
- PASS: `git diff --check`.
- PASS: prod-like compose stack ran backend/frontend/db/nginx on cpuset `0,1` with memory limits 3GiB/1GiB/3GiB/512MiB.
- PASS: running nginx `nginx -T` syntax check after config slice showed `worker_connections 4096`.
- PASS: running backend environment showed `Maximum Pool Size=40;Minimum Pool Size=0`.
- PASS: dev/staging/prod compose config generation showed nginx main config mount and Npgsql pool cap.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` after config slice - Component 118/118, Unit 56/56, Architecture 35/35, Integration 202/202, Contract 1 skipped.

Existing warning observed in .NET validations:
- `AWSSDK.Core` NU1901 low-severity advisory remains pre-existing and unrelated to this work.

## Load Test Results

### Before nginx/pool config slice
Constant 100rps, 30s, `pageSize=12`, current public list-selected read targets:
- Requests: 3,001
- Throughput: 100.0rps
- Failed rate: 0
- p95: 5.3ms
- Dropped iterations: 0

Spike 100 -> 1000 -> 100, 60s peak stage, maxVUs 1000:
- Exit code: 99
- Requests: 35,274
- Average throughput: 440.9rps
- Failed rate: 1.52%
- p95: 1,396.1ms
- Max: 10,260.4ms
- Dropped iterations: 3,725
- Diagnosis: nginx logged `1024 worker_connections are not enough`; post-spike DB diagnostic connection failed with `too many clients already`.

### After nginx/pool config slice
Constant 100rps, 30s, same targets:
- Requests: 3,001
- Throughput: 100.0rps
- Failed rate: 0
- p95: 5.3ms
- Dropped iterations: 0

Spike 100 -> 1000 -> 100, 60s peak stage, maxVUs 1000:
- Exit code: 0
- Requests: 36,122
- Average throughput: 451.5rps
- Failed rate: 0
- p95: 1,502.3ms
- Max: 2,786.7ms
- Dropped iterations: 2,877
- Diagnosis: nginx connection alerts were gone, DB diagnostics worked, and DB activity showed 40 idle backend pool connections plus diagnostic headroom. The remaining gap is throughput/latency saturation, not immediate connection failure.

Artifacts:
- `backend/reports/heavy-detail-read-optimization-2026-05-06/loadtest/k6-2cpu-total8gb-detail-constant-100rps-30s.summary.json`
- `backend/reports/heavy-detail-read-optimization-2026-05-06/loadtest/k6-2cpu-total8gb-detail-spike-100-1000-60s-maxvus1000.json`
- `backend/reports/heavy-detail-read-optimization-2026-05-06/loadtest/k6-2cpu-total8gb-detail-nginx4096-pool40-constant-100rps-30s.json`
- `backend/reports/heavy-detail-read-optimization-2026-05-06/loadtest/k6-2cpu-total8gb-detail-nginx4096-pool40-spike-100-1000-60s-maxvus1000.json`

## Risks And Yellow Flags
- The local prod-like load data uses a fresh seeded local database and current list-selected slugs from that database. It is valid for regression direction, not a perfect replacement for production content with large real posts.
- The standalone k6 helper did not emit p99 because its summary trend stats do not include `p(99)` in this execution path. This should be fixed in load-test tooling.
- Npgsql pool cap protects Postgres from 100-connection exhaustion, but it can move pressure into app-side pool wait under higher load. DB command/open timing should be surfaced to distinguish useful queueing from hidden latency.
- 1000rps spike is still not achieved on the 2CPU/8GiB-total profile. After removing connection failures, the remaining bottleneck is throughput/latency saturation around 450rps average in this local setup.
- Work detail still returns full `ContentJson`. The next structural win is public/admin body model separation or stored public render payloads, not cache.

## Recommendation
Ship this iteration because it removes a request-time body JSON thumbnail parse path, narrows public detail query shape, prevents nginx worker connection collapse, and prevents backend from consuming all Postgres connections. The next iteration should focus on public detail payload/body split and timing instrumentation: DB command/open p95 availability, app serialization timing, and k6 p99 export.
