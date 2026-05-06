# HLS Upload Limit Fix Audit - 2026-05-07

## Summary
- Investigated the reported admin video upload failure shown in the UI as `Failed to process the video as HLS`.
- Reproduced the failure through the real dev nginx/backend path with an authenticated admin session and a 35 MiB multipart upload to `/api/admin/works/{id}/videos/hls-job`.
- Root cause: ASP.NET Core/Kestrel default request body limit was still `30,000,000` bytes, while `WorkVideoPolicy.MaxVideoBytes` allows `200 MiB` and nginx allows `250m`.
- Implemented API startup configuration so Kestrel allows `WorkVideoPolicy.MaxVideoBytes + 1 MiB` multipart overhead and the ASP.NET multipart parser allows `WorkVideoPolicy.MaxVideoBytes`.
- Added a startup regression test so the configured server/form upload limits cannot silently drift below the video policy again.

## Changed
- `backend/src/WoongBlog.Api/Common/ApiServiceCollectionExtensions.cs`
  - Configured `KestrelServerOptions.Limits.MaxRequestBodySize` to `WorkVideoPolicy.MaxVideoBytes + 1 MiB`.
  - Configured `FormOptions.MultipartBodyLengthLimit` to `WorkVideoPolicy.MaxVideoBytes`.
- `backend/tests/WoongBlog.Api.IntegrationTests/StartupCompositionTests.cs`
  - Added `UploadLimits_AllowConfiguredWorkVideoMaximumBeforeAppValidation`.
- `todolist-2026-05-07.md`
  - Added the interrupting HLS upload-limit slice, repro, RED/GREEN steps, and validation results.

## Intentionally Not Changed
- Did not change `WorkVideoPolicy.MaxVideoBytes`; the app-level video policy remains `200 MiB`.
- Did not raise nginx beyond the existing `250m` limit.
- Did not change HLS transcoding behavior, ffmpeg arguments, codec support, or timeline preview generation.
- Did not add caching or alter public load-test target selection.
- Did not commit or merge this interrupting fix yet because it is currently in the same working tree as the runtime-pool-diagnostics WIP.

## Goal Verification
- Goal: identify the critical HLS upload failure.
  - Verified. The first failing layer was Kestrel request body limit, not ffmpeg/HLS processing.
- Goal: make valid policy-sized video uploads reach app validation/transcoding instead of server default limit.
  - Verified by startup config test and live 35 MiB repro changing from HTTP 413 Kestrel failure to HTTP 400 app-level MP4 validation for intentionally invalid bytes.
- Non-goal: prove every codec/large valid MP4 transcodes successfully.
  - Not performed in this slice. That remains a separate ffmpeg/codec validation task if a real MP4 still fails after the body-limit fix.

## Validations
- RED: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~UploadLimits_AllowConfiguredWorkVideoMaximumBeforeAppValidation" --no-restore --logger "console;verbosity=minimal"`
  - Failed before implementation with Kestrel request limit `30000000` below `209715200` plus multipart overhead.
- GREEN: same focused startup/config test passed `1/1` after implementation.
- HLS focused regression: `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~HlsJob_StoresManifestAndProjectsPlaybackUrl|FullyQualifiedName~UploadLimits_AllowConfiguredWorkVideoMaximumBeforeAppValidation" --no-restore --logger "console;verbosity=minimal"`
  - Passed `2/2`.
- WorkVideo unit policy: `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter "FullyQualifiedName~WorkVideoPolicyTests" --no-restore --logger "console;verbosity=minimal"`
  - Passed `39/39`.
- Whitespace: `git diff --check`
  - Passed.
- Dev compose rebuild: `BACKEND_PUBLISH_PORT=18080 docker compose -f docker-compose.dev.yml up -d --build backend`
  - Passed; backend restarted and `/api/health` returned HTTP 200.
- Live repro after rebuild:
  - Same 35 MiB multipart request through `http://127.0.0.1:3000` changed from HTTP 413 with `Request body too large. The max request body size is 30000000 bytes.` to HTTP 400 `{ "error": "Only valid MP4 files are supported." }`.
  - This proves the request now reaches application validation instead of Kestrel's default body cap.
- Backend solution: `dotnet test backend/WoongBlog.sln --no-restore --logger "console;verbosity=minimal"`
  - Passed: Contract `1` skipped, Component `126`, Unit `59`, Architecture `35`, Integration `230`.

## Risks And Follow-Up
- The live repro used a 35 MiB invalid byte payload to prove request-limit behavior. A separate real large MP4 transcode test is still needed if a valid MP4 still fails after deployment.
- Global API request body limit is now about `201 MiB`; endpoint-level limits would be tighter, but the app still has domain validation and nginx remains `250m`.
- Multipart parser limit is aligned exactly with the video policy. Multipart envelope overhead is covered by Kestrel but not counted as file body by `MultipartBodyLengthLimit`.
- Current working tree also contains runtime-pool-diagnostics WIP. Stage/commit this HLS fix carefully if it needs to ship immediately.
- Existing warning remains: `AWSSDK.Core` low severity advisory `GHSA-9cvc-h2w8-phrp`.

## Recommendation
- Ship this HLS upload-limit fix before further backend performance work if production users are blocked on MP4 uploads larger than about 28.6 MiB.
- If the next upload still fails, capture the exact `/videos/hls-job` response body; the next likely class is ffmpeg codec/transcode compatibility, not request size.
