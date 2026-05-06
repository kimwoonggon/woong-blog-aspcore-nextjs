# Public Work Video Read Model - 2026-05-07

## Objective
Reduce the public Work detail hot path when a Work has videos. The previous public detail query loaded the Work row and then queried `WorkVideos`; the target for this slice is one DB command for public Work detail with videos, without cache, without load-test target manipulation, and without lowering public list `pageSize`.

## What Changed
- Added `Work.PublicVideosJson` as a `jsonb` public read-model column.
- Added `WorkPublicVideosReadModel` to serialize and deserialize the small public video snapshot from the canonical `WorkVideo` rows.
- Changed public Work detail projection to select `Works.PublicVideosJson` and build public video DTOs from that stored snapshot.
- Removed the public Work detail `WorkVideos` query from `WorkQueryStore`; admin detail and mutation result paths still use canonical `WorkVideos` rows.
- Added bootstrap patch `20260507_public_work_videos_read_model` to add/backfill `PublicVideosJson` from legacy `WorkVideos` data.
- Updated Work video mutations to refresh `PublicVideosJson` when videos are added, confirmed, HLS-created, deleted, or reordered.
- Updated seed maintenance so seeded Work video rows also keep the public video snapshot current.
- Added/updated tests for schema contract, backfill, public detail single-command contract, public video DTO equivalence, and mutation snapshot refresh.

## Intentional Non-Changes
- Did not add caching. This is a write-time/read-model change, not response caching.
- Did not change load test targets, use seed-only target selection as a production substitute, or reduce `pageSize`.
- Did not remove the canonical `WorkVideos` table; it remains the admin/mutation source of truth.
- Did not change public Work list behavior; this slice targets Work detail with videos only.
- Did not change Blog detail payload or Work/Blog body payload shape in this slice.
- Did not change Npgsql pool size or nginx/app/db timing instrumentation in this slice.

## Expected Backend Test Impact
- Work read target with videos: should improve DB roundtrip count from 2 commands to 1 command.
- Work read target latency: should reduce app elapsed and DB wait variance when Work video rows exist, especially under mixed public API load.
- DB pressure: should reduce per-request command volume for video Work detail, which can lower connection pressure and queue amplification during spikes.
- Throughput: should improve only to the degree Work read with videos is a meaningful part of the selected scenario mix.
- HTTP failures: may improve if failures were caused by DB/pool pressure on video detail reads, but this slice alone is not enough to guarantee 1000rps spike success.
- Not expected to improve Study list/read, Work list, large body serialization, or nginx timing availability.

## Verification
- RED was observed before implementation: the focused PostgreSQL contract failed because `Work.PublicVideosJson` did not exist.
- PASS: focused PostgreSQL contracts 3/3:
  - public Work detail with videos uses one command and does not reference `WorkVideos`.
  - bootstrap schema includes `PublicVideosJson` and patch `20260507_public_work_videos_read_model`.
  - bootstrap backfills `PublicVideosJson` from existing `WorkVideos` rows.
- PASS: component tests 69/69 for public query handlers, Work video mutation handlers, and EF model contracts.
- PASS: integration endpoint tests 21/21 for Work video public/admin flows plus null optional public video field serialization.
- PASS: `git diff --check`.
- PASS: full backend solution test: Contract 1 skipped, Component 124, Unit 56, Architecture 35, Integration 228.
- PASS: latest-code load smoke via local publish mounted into backend runtime image:
  - target: `/api/public/works/seeded-work`
  - rate: 100 rps
  - duration: 10 seconds
  - max VUs: 100
  - requests: 1,001
  - failure rate: 0
  - p95: 3.21 ms
  - summary: `backend/reports/public-work-video-read-model-2026-05-07/loadtest/k6-public-work-detail-100rps-10s.json`

## Yellow Flags
- Docker compose rebuild could not complete in this environment because Docker build DNS failed against NuGet and Google Fonts. This blocked a normal compose rebuild smoke, so the load smoke used local publish mounted into the existing backend runtime image.
- The load smoke is not a prod-like 2CPU/8GB benchmark. It verifies latest-code behavior and gross regression only.
- `PublicVideosJson` is a denormalized read model, so future video mutation paths must refresh it whenever public video fields change.
- Public read still deserializes a small JSON snapshot per request. That is cheaper than an extra DB roundtrip, but it is still a serialization/allocation hotspot for later optimization if profiling shows it.

## Prompt-To-Artifact Checklist
- Write-time representative data model: covered by `Work.PublicVideosJson`, bootstrap backfill, and mutation refresh tests.
- Public request should not query canonical video table: covered by PostgreSQL command-text contract asserting no `WorkVideos` reference.
- Public/admin separation: public detail uses the stored snapshot; admin detail still reads canonical `WorkVideos`.
- DB roundtrip target: Work detail with videos now has a one-command contract.
- Backend tests: focused, component, integration, and full backend solution tests passed.
- Load test: latest-code k6 smoke passed and persisted its summary.
- Audit artifact: this report plus JSON and HTML artifacts are generated.

## Recommendation
Proceed to PR/CI for this slice. The next structural performance slice should target large public detail body payload and serialization allocation because this slice only removes one extra DB command from Work detail with videos.
