# HLS Upload Failure Current Recheck - 2026-05-07

## Scope
- User re-reported a critical `failed to process HLS` failure during video upload.
- This slice rechecked the frontend error path, backend HLS processing path, upload-size fix, Docker/nginx runtime configuration, and tests.
- Production/test code was changed only where needed to fix and guard the newly identified nginx timeout mismatch.

## Finding
- The earlier blocking failure, Kestrel's 30,000,000-byte request body cap, is fixed in the current tree and in the `origin/main` runtime tree.
- A separate critical mismatch was present in nginx runtime configs:
  - backend HLS processing can run for up to `WorkVideos:Hls:TimeoutSeconds = 300` seconds.
  - `/api/admin/works/{id}/videos/hls-job` routes through general `location /api/`.
  - general `location /api/` did not set `proxy_connect_timeout`, `proxy_send_timeout`, or `proxy_read_timeout`.
  - nginx default proxy timeout is therefore a realistic failure point for larger videos or slower 2 CPU shared production runtime.
- Existing 300-second proxy timeout was only applied to `/api/admin/ai/`, which does not cover HLS upload processing.

## Changes
- Added architecture regression coverage in `backend/tests/WoongBlog.Api.ArchitectureTests/NginxRuntimeConfigTests.cs`.
- Added these directives to general `location /api/` blocks:
  - `proxy_connect_timeout 300s;`
  - `proxy_send_timeout 300s;`
  - `proxy_read_timeout 300s;`
- Updated runtime nginx configs:
  - `nginx/default.conf`
  - `nginx/local-https.conf`
  - `nginx/prod-bootstrap.conf`
  - `nginx/prod.conf`

## What Was Intentionally Not Changed
- No ffmpeg command arguments were changed.
- No codec compatibility policy was changed.
- No HLS timeout value was changed.
- No upload size policy was changed.
- No frontend user-facing copy was changed.
- No caching or load-test target shortcut was introduced.

## Validation
- PASS: backend focused HLS integration/config tests passed 2/2.
  - `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter "FullyQualifiedName~HlsJob_StoresManifestAndProjectsPlaybackUrl|FullyQualifiedName~UploadLimits_AllowConfiguredWorkVideoMaximumBeforeAppValidation" --no-restore --logger "console;verbosity=minimal"`
- PASS: frontend HLS error surfacing tests passed 2/2.
  - `npm test -- --run src/test/work-editor.test.tsx -t "surfaces HLS processing errors|sanitizes existing-work HLS upload failures"`
- RED: focused nginx config test failed 4/4 before nginx edits because all runtime `/api/` locations lacked `proxy_connect_timeout 300s;`.
- PASS: focused nginx config test passed 4/4 after nginx edits.
  - `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --filter "FullyQualifiedName~NginxRuntimeConfigTests" --no-restore --logger "console;verbosity=minimal"`
- PASS: full architecture suite passed 39/39.
  - `dotnet test backend/tests/WoongBlog.Api.ArchitectureTests/WoongBlog.Api.ArchitectureTests.csproj --no-restore --logger "console;verbosity=minimal"`
- PASS: static grep confirmed all general `/api/` blocks in the four runtime nginx configs contain the 300-second proxy timeout directives.
- PASS: `git diff --check` passed.
- BLOCKED: local `nginx -t` and compose validation were not run because this WSL environment does not have `nginx` or `docker` installed.

## Runtime Triage Guide
- If production still shows only `Failed to process the video as HLS.`, capture the Network tab response for `POST /api/admin/works/{id}/videos/hls-job`.
- HTTP 413 or `Request body too large` means production is running an old image/config or another proxy layer still caps the body.
- HTTP 504 or nginx upstream timeout means this nginx timeout fix is not deployed/reloaded or another upstream proxy is timing out first.
- JSON `{ "error": "Only valid MP4 files are supported." }` means the upload reached app validation but the file header/content is not accepted as MP4.
- JSON `Unable to process MP4 into HLS: ...` means ffmpeg ran and rejected the source media. The current pipeline uses `-c copy`, so codec/container incompatibility can still fail without re-encoding.
- JSON `HLS processing timed out.` means backend ffmpeg reached the 300-second HLS timeout.

## Final Recommendation
- Ship this nginx timeout fix through `dev` CI, main promotion, and GHCR publish.
- On the server, reload/recreate nginx after deployment and retest the same failing MP4.
- If it still fails after this fix is live, collect the exact HTTP status and response body. The next likely slice is ffmpeg/source-media compatibility, not request size or nginx timeout.
