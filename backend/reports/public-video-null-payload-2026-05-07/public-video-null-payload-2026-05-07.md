# Public Video Null Payload Reduction - 2026-05-07

## Goal
Reduce public Work detail response payload and JSON serialization work for video-heavy detail pages without using cache, seed-only target selection, or `pageSize` shortcuts.

This slice targets nullable optional fields in `WorkVideoDto`. For YouTube or partially populated public videos, several fields are naturally absent. Serializing those fields as explicit `null` increases response bytes and System.Text.Json work without adding renderable information.

## Changed
- Added `JsonIgnoreCondition.WhenWritingNull` to nullable `WorkVideoDto` fields:
  - `playbackUrl`
  - `originalFileName`
  - `mimeType`
  - `fileSize`
  - `width`
  - `height`
  - `duration_seconds`
  - `timeline_preview_vtt_url`
  - `timeline_preview_sprite_url`
- Kept required video fields present: `id`, `sourceType`, `sourceKey`, `sortOrder`, `createdAt`.
- Preserved existing snake_case JSON names for duration and timeline preview URLs when those values are present.
- Added a public endpoint integration test that proves null optional video fields are omitted from public Work detail JSON.

## Intentionally Not Changed
- No cache was added.
- Real Backend Test target selection was not changed.
- Public list `pageSize=12` semantics were not changed.
- DB schema, EF query shape, Npgsql pool size, and nginx configuration were not changed in this slice.
- Public Work detail still returns videos when a Work has videos.
- Public Work/Blog body payload behavior was not changed; heavy body split/read-model work was handled by prior slices and remains a separate capacity boundary.
- Admin mutation/read behavior still uses the same DTO type; frontend parsers already treat missing optional video fields as null-equivalent.

## Goal Verification
- The RED test failed before implementation because null optional video keys were present.
- The GREEN test passed after nullable `WorkVideoDto` fields were annotated to omit null values.
- A rebuilt dev compose stack returned `/api/public/works/seeded-work` with omitted optional null video keys.
- A backend-container k6 smoke against the rebuilt stack completed without HTTP failures.

## Validations
- RED: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~GetWorkBySlug_OmitsNullOptionalVideoFields" --no-restore --logger "console;verbosity=minimal"` failed before implementation.
- GREEN: same focused test passed after implementation.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~PublicEndpointsTests|FullyQualifiedName~PostgresPersistenceContractTests" --no-restore --logger "console;verbosity=minimal"` passed 45/45.
- PASS: `npm test -- --run src/test/public-api-clients.test.ts src/test/public-api-contracts.test.ts` passed 22/22.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"` passed: Contract 1 skipped, Component 124, Unit 56, Architecture 35, Integration 227.
- PASS: `npm run typecheck` passed.
- PASS: `git diff --check` passed.
- PASS: rebuilt dev compose with `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d --build backend nginx`.
- PASS: `curl http://127.0.0.1:3000/api/health` returned HTTP 200.
- PASS: `/api/public/works/seeded-work` payload artifact shows first video omitted `playbackUrl`, `mimeType`, `fileSize`, `width`, `height`, `duration_seconds`, `timeline_preview_vtt_url`, and `timeline_preview_sprite_url` when null.
- PASS: backend-container k6 smoke against `/api/public/works/seeded-work`, 50rps/10s/maxVUs100: 500 requests, failure rate 0, p95 3.43ms.

## Load Smoke
Artifact: `backend/reports/public-video-null-payload-2026-05-07/loadtest/k6-work-detail-50rps-10s.summary.json`

Summary:
- Target: `/api/public/works/seeded-work`
- Rate: 50rps
- Duration: 10s
- Requests: 500
- Failure rate: 0
- p95: 3.43ms
- Payload artifact bytes: 1,195 B
- First video keys after omission: `id`, `sourceType`, `sourceKey`, `originalFileName`, `sortOrder`, `createdAt`

## Expected Backend Test Impact
- Direct expected movement: lower response bytes and JSON serialization work for Work detail responses where videos have null optional fields.
- This should show up in Real Backend Test as lower Work read `Payload P95` for video-heavy targets with missing optional metadata.
- It may slightly reduce app elapsed p95 and allocation pressure under high Work-detail concurrency, but it is not expected to move DB command/open p95 because no query or pool behavior changed.

## Risks And Yellow Flags
- This is a payload compatibility change: clients that incorrectly require explicit `null` keys would need to handle missing optional fields. Existing frontend parser tests passed and current parser already treats missing optional fields as null-equivalent.
- The k6 run is a smoke/regression check, not a 1000rps capacity proof.
- The smoke target uses seeded Work because it deterministically has videos. This does not alter Real Backend Test production target selection.
- Existing `AWSSDK.Core` NU1901 low-severity warnings remain unrelated.

## Recommendation
Promote this small DTO/payload reduction through `dev` CI. After it lands, continue with a measurement-driven heavy detail slice only if Real Backend target metrics show Work read payload or serialization remains material. The next likely higher-impact work is either public body transfer reduction for genuinely huge bodies or Work-video query/read-model reduction if video targets dominate.
