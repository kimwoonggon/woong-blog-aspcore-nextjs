# HLS Copy-Mode Fallback - 2026-05-10

## Summary
Implemented a backend HLS robustness slice for valid MP4 uploads that reach ffmpeg but fail copy-mode HLS packaging.

`FfmpegVideoTranscoder` now keeps the existing fast copy-mode path first. If that path fails, it deletes partial HLS outputs and retries once with a browser-compatible transcode path using H.264 (`libx264`), `yuv420p`, and AAC audio. The fallback runs inside the same overall HLS timeout.

## Changed
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/FfmpegVideoTranscoder.cs`
  - Extracted ffmpeg HLS process execution into reusable helpers.
  - Added copy-mode first execution.
  - Added compatibility transcode fallback only after copy-mode failure.
  - Added partial output cleanup before fallback.
  - Preserved timeout/start-failure behavior and structured backend error messages.
- `backend/tests/WoongBlog.Api.ComponentTests/FfmpegVideoTranscoderComponentTests.cs`
  - Added behavior coverage proving copy-mode failure falls back to compatibility transcode and still emits manifest/timeline artifacts.
- `todolist-2026-05-10.md`
  - Added the HLS fallback slice, user scope adjustment, and validation results.

## Intentionally Not Changed
- No response cache or cache-based mitigation.
- No public Work/Blog read query or DTO behavior changed in this slice.
- No production/remote server deploy path used, per user instruction to ignore remote server.
- No upload size, nginx timeout, or Docker runtime config changed.
- No full Vitest gate completed; it was intentionally stopped after the user redirected validation to homepage e2e.

## Goal Check
- HLS fatal candidate: addressed the backend-code failure mode where valid MP4 input reaches ffmpeg and copy-mode HLS fails.
- Homepage/browser functionality: validated the public/homepage-focused Playwright bundle against local dev compose.
- `pageSize=12`, seed/cache policy: not modified by this slice.
- Full production deploy/load test: explicitly out of scope after user instruction to ignore remote server.

## Validation
- RED: focused fallback component test failed before implementation because copy-mode failure returned `Unable to process MP4 into HLS: copy-mode mux fail...` without fallback.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~FfmpegVideoTranscoderComponentTests --no-restore` passed 1/1.
- PASS: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~WorkVideoEndpointsTests.FfmpegVideoTranscoder_SegmentsHlsAndProducesTheManifest|FullyQualifiedName~WorkVideoEndpointsTests.HlsJob_StoresManifestAndProjectsPlaybackUrl" --no-restore` passed 2/2.
- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --no-restore` passed 130/130.
- PASS: `dotnet test backend/WoongBlog.sln --no-restore` passed Unit 61/61, Component 130/130, Architecture 40/40, Integration 231/231; Contract 1 skipped by pact-file condition.
- PASS: `npm run typecheck` passed.
- PASS: `npm run lint` passed with 0 errors and 12 existing warnings outside the HLS backend change.
- PASS: `git diff --check -- backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/FfmpegVideoTranscoder.cs backend/tests/WoongBlog.Api.ComponentTests/FfmpegVideoTranscoderComponentTests.cs todolist-2026-05-10.md` passed.
- PASS: homepage/public Playwright e2e bundle passed 38/38 against `http://127.0.0.1:3000` dev compose.
- PASS: Playwright latency summary wrote `test-results/playwright/e2e-latency-summary.json` and `.md`; 38 artifacts, 0 budget failures, 1 warning.

## Risks And Follow-Up
- The fallback assumes the deployed ffmpeg build has `libx264` and AAC encoders. The runtime Dockerfile installs ffmpeg, but exact encoder availability should still be checked in deployment images if HLS failures persist.
- Compatibility transcode is CPU-heavy. It runs only after copy-mode failure, so normal compatible uploads still take the fast path.
- This does not fix edge/proxy failures such as 413/504/empty-body responses. Those require runtime/proxy evidence, not backend transcoder changes.
- Full optional/exhaustive Playwright suites were not run; the user requested homepage-function e2e rather than broad low-value gates.

## Recommendation
Promote this HLS backend slice through the normal branch/CI flow, then retest the exact failing video. If it still fails and the response is 413/504/empty, treat it as proxy/runtime. If it returns backend JSON from ffmpeg, use the returned copy/fallback error details for the next HLS media-compatibility slice.

## Clean Branch Revalidation
After isolating this slice onto branch `fix/hls-copy-fallback-20260510` from latest `origin/dev`, the backend checks were rerun in that clean worktree:

- PASS: `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter FullyQualifiedName~FfmpegVideoTranscoderComponentTests --verbosity minimal` passed 1/1.
- PASS: `dotnet test backend/WoongBlog.sln --verbosity minimal` passed Unit 61/61, Component 130/130, Architecture 40/40, Integration 231/231; Contract 1 skipped by pact-file condition.
