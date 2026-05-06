# Public JSON Source Generation - 2026-05-06

## Objective
Reduce CPU/allocation pressure in public backend hot paths by using source-generated System.Text.Json metadata for public response DTOs, without changing public API shape, load-test target realism, cache policy, or database schema.

## Changes Made
- Added `WoongBlogApiJsonSerializerContext` under `WoongBlog.Api.Common.Json`.
- Registered the source-generated context in both Minimal API `HttpJsonOptions` and MVC JSON options.
- Kept the default reflection resolver fallback in the resolver chain for non-hot-path/admin/anonymous payloads not covered by this slice.
- Added startup composition coverage that asserts source-generated metadata exists for public hot-path DTOs:
  - `HomeDto`
  - `PagedBlogsDto`
  - `BlogDetailDto`
  - `PagedWorksDto`
  - `WorkDetailDto`
  - `PageDto`
  - `SiteSettingsDto`
  - Real Backend status/metrics response DTOs

## Intentionally Not Changed
- No cache was added.
- No Real Backend target selection was changed.
- No seeded target priority was introduced.
- No `pageSize=1` shortcut was introduced.
- No public API response field was removed.
- No database schema or EF query structure was changed in this slice.

## Goal Verification
- Public heavy detail read paths already use stored public content/body read-model fields on current `dev`.
- This slice targets the remaining JSON serialization/reflection metadata overhead for public response DTOs.
- Public endpoint response shape remained stable under integration tests.
- Real Backend Test was executed with `pageSize=12` and current public list-selected read targets.

## Validation
- RED: focused startup composition test failed before implementation because `WoongBlogApiJsonSerializerContext` was not present in JSON resolver chains.
- GREEN: focused startup composition test passed after context registration.
- PASS: public endpoint integration tests passed 29/29.
- PASS: full backend solution tests passed: Contract 1 skipped, Component 122, Unit 56, Architecture 35, Integration 224.
- PASS: `npm run typecheck`.
- PASS: `npm run lint` with 0 errors and 6 pre-existing warnings.
- PASS: `git diff --check`.
- PASS: compose rebuild via `BACKEND_PUBLISH_PORT=18080 ./scripts/dev-up.sh`.
- PASS: nginx and backend direct health checks returned HTTP 200.
- PASS: Real Backend k6 smoke `20260506-105311-public-api-rps-3b25399d`: 101 requests, failed 0, average 10.1rps, p95 9.2ms, diagnostics samples 1.
- PASS: Real Backend k6 100rps check `20260506-105431-public-api-rps-264bc24a`: 3,000 requests, failed 0, average 100rps, p95 7.1ms, p99 8.3ms, DB command p95 5.5ms, DB connection-open p95 0ms.

## Risks And Yellow Flags
- This is a structural serialization improvement, not a guaranteed large p95 reduction by itself. It reduces reflection metadata work and should mainly help CPU/allocation under higher concurrency.
- The source-generated context intentionally covers public hot-path DTOs only; admin and long-tail payloads still use reflection fallback.
- The local Real Backend run used the current dev compose environment, not a dedicated 2CPU/8GiB isolated compose project for this slice.
- Existing AWSSDK.Core NU1901 warnings remain unrelated to this change.
- Existing frontend lint warnings remain unrelated to this change.

## Final Recommendation
Promote this slice through `dev` CI. After it lands, continue the next iteration on measured heavy detail pressure: either source-generated context coverage expansion if serialization remains hot, or direct payload/body projection reduction if app elapsed p95 remains high while DB metrics stay normal.
