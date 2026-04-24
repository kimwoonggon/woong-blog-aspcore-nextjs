# Video Preview Final Stabilization Audit

## Summary
- Fixed `WorkVideoPlayer` hover preview math to use the actual progress overlay rect and frame-relative positioning.
- Added a runtime readiness signal (`data-preview-ready`) so browser tests can wait for parsed preview cues instead of racing the VTT fetch.
- Corrected Playwright specs to:
  - handle raw snake_case preview fields from direct API responses
  - scroll the hover target into view before hit-testing
  - wait for preview assets/readiness instead of false-skipping
- Fixed backend short-video preview generation for 1-second uploads by using a single-frame preview path when only one preview frame is needed.
- Stopped persisting preview storage keys when preview assets were not actually generated, preventing broken `timeline.vtt` / `timeline-sprite.jpg` URLs.

## Intentionally Not Changed
- Did not reintroduce autoplay or muted autoplay.
- Did not switch back to browser-native video controls; the current hover preview depends on the custom in-frame progress overlay.
- Did not backfill or reprocess older work rows that were already saved with broken preview URLs before this fix.

## Goal Check
- Goal: frontend hover preview works in live browser. Verified.
- Goal: backend preview assets for new uploads are actually generated and published. Verified for new 1-second `sample-video.mp4` uploads.
- Goal: recording Playwright spec is green and writes a stable `.webm`. Verified.
- Non-goal: redesign the entire player control set or change playback policy. Not changed.

## Validations Performed
- `npx vitest run src/test/work-video-player.test.tsx`
- `npx vitest run src/test/work-editor.test.tsx src/test/work-video-player.test.tsx src/test/blog-notion-workspace.test.tsx src/test/auth-csrf.test.ts`
- `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter WorkVideoHlsJobPlanTests`
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter WorkVideoEndpointsTests`
- `npm run typecheck`
- `npm run lint` (warnings only, no errors)
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/public-work-videos.spec.ts --grep "PF-044" --workers=1`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/video-preview-recording-0424.spec.ts --workers=1`
- `curl` verification for new short-upload work `public-video-preview-1777023334596`:
  - `/media/.../timeline.vtt` -> `200 text/vtt`
  - `/media/.../timeline-sprite.jpg` -> `200 image/jpeg`

## Risks / Yellow Flags
- Older short-video rows created before this fix can still point at preview URLs that 404. They need re-upload or a reprocessing path if you want historical consistency.
- The recording spec copies a stable `.webm` artifact in `afterEach`; running multiple Playwright processes that write the same artifact path in parallel can race. Serial execution is the safe path for that recording spec.
- Existing lint warnings remain in unrelated backup/scripts/test files.

## Final Recommendation
- Keep the recording spec serialized.
- If historical works also need hover preview, add an admin or batch reprocessing command for existing HLS videos created before this fix.
