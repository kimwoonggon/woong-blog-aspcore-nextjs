# Next Plan After CI - Public Work Visible DTO Trim

## CI State
- PR: `#131` - `Trim public work visible DTO fields`
- Branch: `perf/public-work-visible-dto-trim-20260510`
- Base: `dev`
- CI run: `25623888713`
- Result: passed
- Jobs passed: Backend architecture tests, Backend integration tests, Backend unit tests, Backend component tests, Frontend lint/types/unit tests, Compose Dev Verification, Pact provider verification, Browser smoke.
- Notes: GitHub Actions reported Node 20 deprecation annotations for upstream actions. These were informational and did not fail the gate.

## Immediate Promotion Plan
1. Merge PR `#131` into `dev`.
2. Watch the `dev` push CI gates:
   - `CI Dev`
   - `Publish GHCR Dev`
   - `Promote Main Runtime`
3. If the promotion workflow opens a main promotion PR, merge it after its checks are green.
4. Watch the `main` gates:
   - main branch CI
   - `Publish GHCR Main`
5. Do not treat the task as complete until dev and main are both verified.

## Runtime Verification Plan
After the promoted image is deployed, run production preflight before interpreting load-test numbers.

Recommended environment:
```bash
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1
WORK_READ_PATH=/api/public/works/<real-video-work-slug>
REQUIRE_ADMIN_DIAGNOSTICS=1
```

Use `REQUIRE_ADMIN_DIAGNOSTICS=1` only when a valid admin cookie is available.

## Real Backend Retest Plan
Keep the test realistic and comparable:
- Keep `pageSize=12`.
- Do not use seed shortcuts.
- Do not add response cache as part of this measurement.
- Test under the real deployment constraint: 2 total CPUs and 8 GB total memory shared by backend, frontend, nginx, and database.
- Use real public Work/Study list/read URLs. Avoid intentionally huge one-off content unless the goal is heavy-detail diagnosis.

## Decision Rules For The Next Backend Slice
- If target DB command p95/count is high: optimize query shape, index coverage, or connection/pool pressure for that target.
- If DB command p95 is low but ASP.NET app elapsed is high: continue payload, serialization, allocation, and body-shaping work.
- If runtime turns red mainly from GC or ThreadPool while DB stays low: investigate app-side allocation and large JSON/body rendering.
- If one Work read dominates the run: inspect that Work payload and projection path separately before changing global behavior.
- If HLS upload still reports `failed to process HLS`: treat it as a separate fatal upload slice with a reproduction-first test, because it is not proven to be the same bottleneck as public read load.

## Probable Next Work
The next backend performance slice should be chosen from the promoted production metrics, not from local assumptions. Current likely candidates are:
- HLS upload fatal root-cause fix if upload remains broken.
- Public detail body/serialization allocation reduction if app elapsed dominates while DB is low.
- Public list index maintenance only if production target DB metrics show list DB cost remains material.
