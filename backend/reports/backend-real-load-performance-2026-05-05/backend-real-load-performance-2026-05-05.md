# Backend Real Load Performance Audit - 2026-05-05

> Correction 2026-05-06: the Real Backend target-selection and k6 evidence in this report used forced `seeded-work`/`seeded-blog` read targets and `pageSize=1` list targets. That workload is not valid as realistic production-load evidence. Treat the load-test evidence and recommendations in this report as superseded by `backend/reports/real-backend-target-reality-correction-2026-05-06/real-backend-target-reality-correction-2026-05-06.md`.

## Goal
Improve public backend real load-test performance without using server-side response/output caching, and validate under a Docker condition close to the production server: total server capacity of 2 CPUs and about 8 GiB memory shared by backend, nginx, DB, and frontend.

## Changed
- Optimized public Blog list reads to project only card fields before materialization, excluding full `ContentJson` from list responses.
- Optimized public Work list reads to project only card fields and fetch work videos only for rows that require video thumbnail fallback.
- Optimized public Work detail reads to trust persisted timeline preview metadata and skip per-request storage/object existence checks. Admin Work detail still verifies preview objects.
- Superseded 2026-05-06: the Real Backend Test default target change to deterministic seeded content and `pageSize=1` list targets was incorrect for realistic load testing and has been reverted.
- Added/updated tests for public/admin preview verification behavior and deterministic load-test target paths.
- Created a Docker load-test override for a dedicated `wb-perf` stack with all app services pinned to CPU set `0,1`, Production backend environment, Warning-level EF logs, and memory limits whose sum is below 8 GiB.

## Final Docker Shape
Compose cannot enforce one project-wide shared memory cap directly, so the production 8 GiB total was approximated by splitting limits across services:

| Service | CPU | Memory Limit |
| --- | --- | --- |
| backend | `0,1` | 3 GiB |
| db | `0,1` | 3 GiB |
| frontend | `0,1` | 1 GiB |
| nginx | `0,1` | 512 MiB |

Total configured service memory is about 7.5 GiB, leaving headroom for runtime/container overhead.

## Intentionally Not Changed
- No output cache, response cache, CDN cache, or server-side response memoization was added.
- No database schema/index changes were added; existing published/date and search indexes are already present.
- No production compose or env deployment changes were made in this performance slice.
- Superseded 2026-05-06: default load-test targets must not be intentionally deterministic/lightweight by forcing seed content. Real Backend Test now keeps fetched public-content order and list `pageSize=12`.

## Validation
- `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter "FullyQualifiedName~PublicQueryHandlerComponentTests" --no-restore` passed 34/34. Existing `AWSSDK.Core` NU1901 warning remains.
- `npm test -- --run src/test/load-test-dashboard.test.ts` passed 26/26.
- `npm run typecheck` passed.
- `npm run lint` passed with six existing unrelated warnings and no warnings from this task's files.
- `git diff --check` passed.
- `rg -n "OutputCache|CacheOutput" backend/src src || true` returned no cache implementation references.
- Docker health check on the prod-like stack returned HTTP 200.
- `docker inspect` confirmed backend/db/frontend/nginx `cpuset=0,1` and split memory limits.
- PR #39 CI passed on GitHub Actions runs `25382579122` and `25382588680`: backend architecture/component/integration/unit, frontend lint/types/unit, compose dev verification, pact provider verification, and browser smoke.

## Load Test Evidence
Superseded 2026-05-06: these measurements used forced seeded read targets and `pageSize=1` list targets. They are retained only as historical evidence of the invalid workload shape and must not be used as current realistic backend capacity proof.

External k6 was run in a separate container on the same Docker network so the load generator did not consume the backend service's pinned 2 CPUs.

| Scenario | Result | Interpretation |
| --- | --- | --- |
| 300 rps constant, 30s | 9,001 requests, 300.01 rps, failed 0, dropped 0, p95 4.67 ms | Green |
| 400 rps constant, 30s | 12,000 requests, 400.01 rps, failed 0, dropped 0, p95 5.29 ms | Green |
| 450 rps constant, 30s | 13,501 requests, 450.01 rps, failed 0, dropped 0, p95 14.51 ms | Green |
| 500 rps constant, 30s | 15,001 requests, 500.00 rps, failed 0, dropped 0, p95 338.33 ms | Yellow latency boundary |
| 100 -> 1000 -> 100 spike, pre-final memory split check | 26,826 requests, 447.11 rps, failed 0, dropped 1,173, p95 1,460.44 ms | Red/capacity test |

## Interpretation
- Superseded 2026-05-06: the previous interpretation incorrectly treated arbitrary heavy content targets as something to avoid. Heavy Work/Study detail pages are realistic read targets; the fix is to keep list DTOs light while allowing detail targets to remain realistic.
- Superseded 2026-05-06: deterministic seeded read targets and smaller list targets are not valid proof for realistic public API load.
- Public Work read avoids repeated preview object existence checks, removing a risky per-request external/object-storage dependency from the public hot path.
- Under the corrected total-server Docker model, the deterministic public API workload is low-latency green through 450 rps.
- 500 rps now sustains throughput but has a large p95 jump, so it should be treated as a stress/yellow boundary rather than a green target.
- 1000 rps spike remains outside current 2 CPU capacity. Raising `maxVUs` exposes saturation; it does not increase server throughput.

## Risks And Follow-Up
- The compose override approximates an 8 GiB total server by splitting per-container memory limits. It is not identical to a single Docker daemon/VM memory cap.
- Local WSL/Docker scheduling can still vary between runs, so production confirmation should use the real prod host with the same deterministic targets.
- To push beyond 450 rps green on 2 CPU, next work should focus on reducing DB round trips in detail endpoints, checking Npgsql pool behavior under arrival-rate bursts, and measuring nginx/backend/db CPU contention separately.
- If 1000 rps spike remains a hard requirement on 2 CPU, this likely needs deeper architectural changes or more CPU, not just `maxVUs` tuning.

## Recommendation
Use 300-450 rps as the current green zone for deterministic public real backend tests on a 2 CPU/about 8 GiB total server. Treat 500 rps as the first yellow stress boundary and 1000 rps spike as a red/capacity test until a separate bottleneck-reduction task proves otherwise.
