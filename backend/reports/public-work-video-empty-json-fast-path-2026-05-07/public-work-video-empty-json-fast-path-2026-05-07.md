# Public Work Video Empty JSON Fast Path - 2026-05-07

## Objective
Remove avoidable JSON deserialization allocation from the common no-video public Work detail path introduced by the stored public video snapshot read model.

## What Changed
- Added an empty-array fast path in `WorkPublicVideosReadModel.Deserialize`.
- `null`, whitespace, and normalized `[]` snapshots return `Array.Empty<WorkPublicVideoSnapshot>()` without calling `JsonSerializer.Deserialize`.
- Added a unit contract to lock the no-allocation empty snapshot behavior.

## Intentional Non-Changes
- Did not change the public API response shape.
- Did not change DB schema or stored data.
- Did not change canonical `WorkVideos` behavior.
- Did not add cache.

## Expected Backend Test Impact
- No-video Work detail: removes one small JSON deserialization allocation per public detail request.
- Work detail with videos: unchanged; still deserializes the stored snapshot because actual video DTOs are needed.
- Mixed public load: improvement is small but positive when Work read targets commonly have no videos.
- 1000rps spike success is not guaranteed by this micro-slice; it only reduces allocation pressure on one common branch.

## Verification
- RED: focused unit test failed because `Deserialize("[]")`, whitespace-wrapped `[]`, and newline/tab-wrapped `[]` returned newly deserialized empty arrays.
- PASS: focused unit test passed 3/3 after the fast path.
- PASS: focused PostgreSQL no-video public Work detail contract passed 1/1.
- PASS: full unit test suite passed 59/59.
- PASS: `git diff --check`.
- PASS: full backend solution test: Contract 1 skipped, Component 124, Unit 59, Architecture 35, Integration 228.
- PASS: latest-code k6 smoke via local publish mounted into backend runtime image:
  - target: `/api/public/works/seeded-work-extra-01`
  - rate: 100 rps
  - duration: 10 seconds
  - max VUs: 100
  - requests: 1,001
  - failure rate: 0
  - p95: 2.22 ms
  - summary: `backend/reports/public-work-video-empty-json-fast-path-2026-05-07/loadtest/k6-public-work-no-video-100rps-10s.json`

## Yellow Flags
- This is a small allocation optimization, not a structural payload split.
- The load smoke is not a prod-like 2CPU/8GB benchmark.
- Bigger remaining backend test gains still require public detail body/serialization and timing attribution work.

## Recommendation
Merge this fast-path slice after CI. Continue with a larger slice targeting heavy public detail body payload and response serialization, or nginx/app/db timing attribution if measurement gaps block prioritization.
