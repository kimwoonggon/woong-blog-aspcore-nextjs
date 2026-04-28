# Backend WorkVideo R2/HLS Failure Coverage Audit - 2026-04-26

## Summary

This is an audit-only report. No production code and no tests were modified.

Backend coverage is strong at the aggregate level, but WorkVideo operational risk is still concentrated in R2/object-storage behavior, storage selection branches, and HLS failure side effects. The main risk is not raw line coverage. The risk is whether failures leave bad database state, orphan objects, missing cleanup jobs, or user-visible videos that point at incomplete media.

Recommendation: do not use real R2 in tests. Start with deterministic fakes and temp directories. Add a very small S3-client seam only if testing `R2VideoStorageService` itself is required beyond presigned URL construction and unconfigured behavior.

## Sources Inspected

- `coverage/backend/full/report/Summary.json`
- `backend/src/WoongBlog.Api/Modules/Content/Works/WorkVideos/WorkVideoEndpoints.cs`
- `backend/src/WoongBlog.Application/Modules/Content/Works/WorkVideos/*`
- `backend/src/WoongBlog.Infrastructure/Storage/*`
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/*`
- `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkVideo*Store.cs`
- Existing tests under:
  - `backend/tests/WoongBlog.Api.UnitTests`
  - `backend/tests/WoongBlog.Api.ComponentTests`
  - `backend/tests/WoongBlog.Api.IntegrationTests`

Specialist skill search was run with `npx skills find dotnet s3 testing`; no external skill was installed because returned skills had low install counts.

## 1. Current Coverage Snapshot

Current full backend coverage from `coverage/backend/full/report/Summary.json`:

| Scope | Line coverage | Branch coverage | Notes |
| --- | ---: | ---: | --- |
| Full backend | 92.2% | 70.5% | Aggregate is healthy, but failure-path risk remains uneven. |

WorkVideo/R2/HLS target snapshot:

| Target | Line coverage | Branch coverage | Covered lines | Branches | Risk read |
| --- | ---: | ---: | ---: | ---: | --- |
| `R2VideoStorageService` | 9.0% | 0.0% | 4/44 | 0/8 | Critical gap. Real object operations, 404 behavior, HLS-prefix delete, and presign variants are effectively untested. |
| `LocalVideoStorageService` | 91.4% | 70.0% | 32/35 | 7/10 | Good happy-path/temp-dir coverage; remaining risk is path/delete edge behavior and missing files. |
| `WorkVideoStorageSelector` | 78.2% | 30.0% | 18/23 | 3/10 | Branch gap around environment, forced R2, unconfigured R2, and fallback selection. |
| `WorkVideoPlaybackUrlBuilder` | 100.0% | 66.6% | 23/23 | 8/12 | Line-complete but branch-thin for malformed HLS source keys, missing storage, and R2/local variants. |
| `FfmpegVideoTranscoder` | 61.7% | 50.0% | 21/34 | 3/6 | Happy HLS path covered; process start, non-zero exit, timeout, missing manifest, ffprobe/preview failures remain thin. |
| `WorkVideoService` | 100.0% | n/a | 6/6 | 0/0 | Coverage hides behavior risk because the class delegates heavily through storage and cleanup store side effects. |
| `WorkVideoCleanupStore` | 100.0% | n/a | 4/4 | 0/0 | Summary does not expose branch detail; idempotency and source-key parsing still need direct behavior tests. |
| `WorkVideoCommandStore` | 100.0% | n/a | 29/29 | 0/0 | Basic persistence is covered indirectly; concurrency and failed-save behavior are not the focus here. |
| `WorkVideoQueryStore` | 92.3% | 66.6% | 12/13 | 4/6 | Timeline preview URL and storage-object URL branches remain worth targeted coverage. |
| `WorkVideoEndpoints` | 100.0% | 100.0% | 132/132 | 6/6 | Endpoint mapping is covered, but endpoint coverage does not prove storage/HLS failure side effects. |
| `WorkVideoHlsJobPlan` | 100.0% | 100.0% | 43/43 | 4/4 | Good unit coverage for path planning and optional timeline preview keys. |
| `StartWorkVideoHlsJobCommandHandler` | 100.0% | n/a | 8/8 | 0/0 | Summary reports green lines, but important collaborator failure paths are not visible as branch coverage. |
| `CloudflareR2Options` | 100.0% | 50.0% | 7/7 | 4/8 | `IsConfigured()` is only partly covered; there is no separate R2 options validator. |

Notes:

- `WorkVideoHlsOutputPublisher`, `WorkVideoHlsWorkspace`, and `WorkVideoFileInspector` were inspected but are not listed as separate classes in the current `Summary.json`. Their behavior should still be covered through direct component tests because they own important file/object side effects.
- The previous `WorkVideoPolicy` hotspot has been addressed separately and is now 100.0% line / 95.0% branch in the full report.

## 2. Production Behavior Inventory

### Issue Upload

Endpoint: `POST /api/admin/works/{id}/videos/upload-url`

Flow:

1. Validate admin authorization and request payload.
2. Load work for update.
3. Reject missing work, stale `VideosVersion`, invalid file metadata, or max video count.
4. Resolve storage type through `WorkVideoStorageSelector`.
5. Create `WorkVideoUploadSession` with `Issued` status and one-hour expiry.
6. Save the session.
7. Ask the selected storage to create an upload target.

Important failure risk: the session is saved before `CreateUploadTargetAsync` is called. If R2 presigned URL generation throws, an issued upload session can remain without a usable upload target.

### Confirm Upload

Endpoint: `POST /api/admin/works/{id}/videos/confirm`

Flow:

1. Load work and upload session.
2. Reject missing work, stale version, missing session, or expired session.
3. Resolve storage by the session's `StorageType`.
4. Read object metadata with `GetObjectAsync`.
5. Check object exists, MIME matches for non-local storage, size matches, and MP4 signature is valid.
6. Reject max video count.
7. Add `WorkVideo`, mark session `Confirmed`, increment `VideosVersion`, and return projected video list.

Important failure risk: R2 metadata and prefix reads are untested. Bad R2 content type, size drift, missing object, and invalid prefix must not create a video or increment `VideosVersion`.

### Local Upload

Endpoint: `POST /api/admin/works/{id}/videos/upload?uploadSessionId=...`

Flow:

1. Parse `uploadSessionId`.
2. Read multipart form and file.
3. Reject missing file, missing session, non-local session, invalid file metadata, or missing local storage.
4. Save file to local media root.
5. Mark session `Uploaded`.

Important failure risk: if local file save throws, session should remain `Issued` and no partial DB transition should occur. Partial files may exist depending on filesystem failure timing; no cleanup is scheduled by current code.

### R2 Upload / Presigned Upload

Flow:

1. `R2VideoStorageService.CreateUploadTargetAsync` validates R2 config.
2. Builds a `GetPreSignedUrlRequest` with bucket, key, `PUT`, 30-minute expiry, and content type.
3. Uses `BrowserEndpoint` for signing if set; otherwise uses `Endpoint`.
4. Browser uploads object directly to R2.
5. Server later confirms by reading metadata and a prefix from R2.

Important failure risk: no real network should be used in tests. Current `R2VideoStorageService` constructs `AmazonS3Client` internally, so object operation tests cannot use a clean fake S3 client without a small seam.

### Add YouTube Video

Endpoint: `POST /api/admin/works/{id}/videos/youtube`

Flow:

1. Load work.
2. Reject missing work, stale version, max video count, or invalid YouTube ID/URL.
3. Add a YouTube `WorkVideo`.
4. Increment `VideosVersion`.

Operational storage risk is low. It is still part of lifecycle ordering and max-count behavior.

### Reorder Videos

Endpoint: `PUT /api/admin/works/{id}/videos/order`

Flow:

1. Load work and existing videos.
2. Reject missing work, stale version, or order payload that does not include every video exactly once.
3. Temporarily moves sort orders out of the way, saves, then writes final order.
4. Increment `VideosVersion`.

Storage risk is none; DB side-effect risk is partial failure between the two saves.

### Delete Video

Endpoint: `DELETE /api/admin/works/{id}/videos/{videoId}?expectedVideosVersion=...`

Flow:

1. Load work and video.
2. Reject missing work, stale version, or missing video.
3. Enqueue cleanup unless source is YouTube.
4. For HLS source keys, parse the embedded storage type and manifest storage key.
5. Remove the video, compact remaining sort orders, increment `VideosVersion`.

Important failure risk: cleanup enqueue is part of the same save as video deletion. Invalid HLS source keys silently skip cleanup. HLS cleanup uses the manifest key as the cleanup key and relies on storage delete behavior to remove the whole HLS prefix.

### Start HLS Job

Endpoint: `POST /api/admin/works/{id}/videos/hls-job`

Flow:

1. Read multipart form and parse `expectedVideosVersion`.
2. Reject missing file, missing work, stale version, invalid file metadata, max videos, or missing storage.
3. Create `WorkVideoHlsJobPlan`.
4. Create temp workspace and copy uploaded source.
5. Validate MP4 signature from workspace source file.
6. Run HLS transcoder.
7. Publish every file in the HLS directory to selected storage.
8. Add an HLS `WorkVideo`, optionally with timeline preview keys if both VTT and sprite exist.
9. Increment `VideosVersion`.

Important failure risk: if publishing fails after some files were stored, the DB row is not added, but partial HLS objects/files can remain and no cleanup job is scheduled.

### HLS Transcode

Class: `FfmpegVideoTranscoder`

Flow:

1. Starts `ffmpeg` with HLS arguments.
2. Timeout is enforced with linked cancellation.
3. Non-zero exit returns a user-facing error, including stderr when available.
4. Success triggers best-effort timeline preview generation.
5. Success still fails if manifest was not produced.

Important failure risk: start failures, timeout, missing manifest, stderr/no-stderr variants, ffprobe invalid output, and preview failures are not strongly covered.

### Upload / Store Manifest

Class: `WorkVideoHlsOutputPublisher`

Flow:

1. Enumerate files in HLS directory in ordinal order.
2. Use `application/vnd.apple.mpegurl` for `.m3u8`.
3. Store to selected `IVideoObjectStorage` under the planned HLS prefix.

Important failure risk: if manifest upload fails after segments or preview artifacts were uploaded, DB state should not change and cleanup should be considered for partial objects. Current code does not schedule cleanup for partial publish failures.

### Upload / Store Segments

Class: `WorkVideoHlsOutputPublisher`

Flow:

1. Uses `video/mp2t` for non-manifest, non-VTT, non-JPG files.
2. Stores each file through `SaveDirectUploadAsync`.

Important failure risk: partial segment upload creates incomplete HLS prefixes. There is no rollback or cleanup scheduling in current code.

### Timeline Preview Artifacts

Classes: `FfmpegVideoTranscoder`, `WorkVideoHlsOutputPublisher`, `WorkVideoHlsJobPlan`

Flow:

1. Probe duration with `ffprobe`.
2. Generate sprite with `ffmpeg`.
3. Write `timeline.vtt` only if sprite exists.
4. Publish VTT and JPG with specific content types.
5. Add timeline preview storage keys only when both artifacts exist.

Important failure risk: preview failure should not fail the HLS job, but it should omit timeline preview keys. That behavior is only partly covered.

### Cleanup

Classes: `WorkVideoCleanupStore`, `WorkVideoService`, `VideoStorageCleanupWorker`, storage services

Flow:

1. Deleting videos or works enqueues cleanup jobs for non-YouTube storage.
2. HLS source keys are parsed into underlying storage type plus manifest key.
3. Expired unconfirmed upload sessions are marked `Expired` and enqueue cleanup.
4. Processing cleanup jobs calls storage `DeleteAsync`.
5. Missing storage marks job `Failed`.
6. Storage exceptions increment attempts and leave `Pending` until attempt 5, then `Failed`.
7. Successful delete marks `Succeeded`.

Important failure risk: HLS prefix deletion for R2 is untested. Duplicate cleanup jobs are guarded for pending jobs only; idempotency should be explicit.

## 3. Failure Path Matrix

| Lifecycle stage | Current tests | Missing failure tests | Expected behavior | DB state should change? | Storage files/objects should exist? | Cleanup scheduled? | Recommended level | Fake R2/S3 seam already available? |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Issue upload | Integration covers missing work and invalid metadata. Local happy path creates upload target. | Stale version, max videos, selector returns unavailable storage, storage target creation throws, R2 selected/configured/unconfigured branches. | Return 404/409/400 for known validation failures. Storage target failures should not create a durable usable session; current code likely leaves an `Issued` session if target creation throws after save. | Success creates `WorkVideoUploadSession` only. Known validation failures should not. Target creation failure should ideally not leave active session. | None. | No cleanup on normal issue. If a target failure leaves a session, later expiry cleanup is the only safety net. | Component for handler with fake storage; integration for endpoint mapping only. | Handler-level fake storage yes through `IVideoObjectStorage`; real `R2VideoStorageService` fake no. |
| R2 upload / presigned upload | Startup composition confirms R2 service is registered and testing options are unconfigured. No R2 upload behavior test. | Configured presigned URL shape, `BrowserEndpoint` branch, unconfigured throws, signing failure, no session leak on signing failure. | Generate deterministic PUT target without network. Do not call real R2. On signing failure, return controlled failure and avoid active orphan session. | Success creates upload session. Failure should not create or should expire/cleanup session. Current code is risky. | Browser upload is external; server test should not create objects. | No cleanup unless a session/object later expires. | Unit/component. | Presign can be exercised without real network, but deterministic fake signing requires a seam. |
| Local upload | Integration covers missing file and local happy upload. Component covers local save/read/delete. | Missing uploadSessionId endpoint parse, missing session, non-local session, invalid metadata during upload, local storage missing, `SaveDirectUploadAsync` throws, partial file behavior. | Reject bad requests without marking session uploaded. If file save throws, session remains `Issued`; no video row. | Only success should set session `Uploaded`. | Success creates file. Failure should not create file; partial filesystem failure must be understood. | No cleanup currently on failed local save. | Component for handler with fake storage; integration for missing query parse. | Not R2-specific; fake storage available. |
| Confirm upload | Integration covers local happy confirm. | Missing session, expired session, storage backend missing, object missing, R2 MIME mismatch, size mismatch, invalid MP4 prefix, max videos reached after upload, stale version, storage metadata/read throws. | Known invalid cases return bad request/not found/conflict. No video row, no version increment. Expired session should become `Expired`. | Only success creates `WorkVideo`, marks session `Confirmed`, increments version. Expiry changes session to `Expired`. | Existing uploaded object remains on validation failure. | No cleanup currently for failed confirm except expiry path; consider cleanup for expired sessions. | Component with fake storage/file inspector for side effects; integration for one endpoint failure. | Handler-level fake storage yes; real R2 object metadata fake no. |
| Add YouTube video | Integration covers successful add and projection. Policy unit tests cover parser. | Missing work, stale version, max videos, invalid input through endpoint result, no DB mutation on invalid input. | Return 404/409/400. No storage effects. | Success adds row and increments version. Failure should not. | None. | None. | Component/integration. | Not applicable. |
| Reorder videos | Integration covers stale version, invalid IDs, public/admin order. Component covers deterministic order rewrite. | Missing work, partial failure between first and second save, duplicate IDs specifically, empty order when videos exist. | Return 404/409/400. Failed reorder should not leave temporary sort order. | Success updates sort order and version. Failure should not. | None. | None. | Component; transaction behavior may need integration if changed. | Not applicable. |
| Delete video | Integration covers missing video and local cleanup job. Component covers HLS delete enqueue and sort compaction. | Missing work, stale version, YouTube delete should not enqueue cleanup, invalid HLS source key should be visible/guarded, duplicate pending cleanup idempotency, cleanup enqueue failure. | Delete removes row, compacts order, increments version, and enqueues cleanup for local/R2/HLS. YouTube should not enqueue. | Success removes row and increments version. Failure should not. | Object remains until cleanup worker runs. | Yes for local/R2/HLS; no for YouTube. HLS should schedule manifest key for underlying storage. | Component for command/store; integration for endpoint status. | Fake storage not needed for enqueue; R2 delete behavior needs seam. |
| Start HLS job | Integration covers local HLS success. HLS plan unit tests cover path plan and optional preview keys. | Missing file, missing work, stale version, invalid metadata, max videos, selector/storage unavailable, invalid MP4 source, workspace copy failure, transcoder error, publish failure. | Validation/transcode/publish failures should return bad request or controlled failure. No video row and no version increment. Workspace should be disposed. Partial published output is a serious orphan risk. | Success adds HLS row and increments version. Failures before save should not change work/video state. | Success creates manifest/segments/preview objects. Transcode failure should not store output. Publish failure can leave partial objects today. | No cleanup scheduled for HLS publish failure today; this is a gap. | Component with fake workspace/transcoder/publisher/storage; selected integration for endpoint form parsing. | Handler-level fake storage yes; real R2 publish fake no. |
| HLS transcode | Integration covers fake ffmpeg success and preview generation. | ffmpeg executable missing, `Start()` failure if feasible, timeout, non-zero exit with stderr, non-zero exit without stderr, success but missing manifest, cancellation behavior, ffprobe non-zero, invalid duration, preview ffmpeg failure. | Return specific error string for hard transcode failures. Preview failure should not fail job. Missing manifest should fail. Timeout should kill process. | Transcoder alone no DB. In handler, failures should not add video or increment version. | Failure should not require stored objects. Local temp files may exist until workspace dispose. | No cleanup needed unless publish started. | Integration/component with fake shell tools and temp dirs. | Not R2-specific. |
| Upload/store manifest | HLS endpoint success proves manifest can be stored locally and served. | Publisher content type for manifest, manifest upload throws, manifest missing from output, manifest uploaded after partial segments, storage unavailable. | Failure should stop before DB row is added. Partial objects should be cleaned or scheduled for cleanup. | No DB change on publish failure. | Partial objects may exist today. Success stores manifest. | Should schedule cleanup for partial HLS prefix, but current code does not. | Component for publisher with fake storage; handler component for publish failure. | Fake storage yes through `IVideoObjectStorage`; real R2 service no. |
| Upload/store segments | HLS endpoint success asserts one segment appears in manifest. | Segment content type, segment upload throws, multiple segments ordering, partial segment set. | Failure should not add HLS video. Partial segment objects should not be left indefinitely. | No DB change on failure. | Partial objects may exist today. Success stores all segments. | Should schedule cleanup for partial prefix on failure, but current code does not. | Component. | Fake storage yes through `IVideoObjectStorage`; real R2 service no. |
| Timeline preview artifacts | HLS success covers VTT/sprite projection; plan unit covers omitting preview keys when asked. | ffprobe missing/invalid, preview ffmpeg failure, only VTT or only sprite exists, publisher content types for VTT/JPG, no-preview success omits keys in handler. | Preview failure should allow HLS video but omit timeline preview URLs/keys. VTT/JPG content types should be correct. | Success adds video. Preview absence should not block DB row. | Success may store preview objects. Preview failure should not store incomplete preview pair. | No cleanup unless overall HLS job fails after partial publish. | Unit/component for transcoder and publisher; component for handler. | Fake storage yes through `IVideoObjectStorage`. |
| Cleanup processing | Component covers local delete success, missing storage failure, retry to max attempts, and session expiry cleanup. Local service covers HLS directory delete. | R2 delete single object, R2 HLS prefix delete with pagination, pending retry below max remains pending, no-job case, max batch size 20, duplicate cleanup jobs, YouTube no-op, malformed HLS source key no-op, local missing file delete idempotency. | Success marks `Succeeded`; missing storage marks `Failed`; transient exceptions retry until attempt 5; cleanup enqueue should be idempotent for pending jobs. | Job status/attempts change. Expiry changes session status. | Success should remove local files/R2 objects. Missing objects should be idempotent where storage supports it. | Cleanup is the scheduled action. Duplicate pending jobs should not be created. | Component/store tests; R2 service tests after seam. | Store/service fake storage yes; real R2 deletion fake no. |

## 4. Fake / Seam Assessment

### Can `R2VideoStorageService` Be Tested Without Real R2?

Partially.

What is testable now without real R2:

- `BuildPlaybackUrl` for configured and unconfigured options.
- `IsConfigured` behavior through `CloudflareR2Options`.
- `CreateUploadTargetAsync` may generate a presigned URL without network because AWS SDK signing is local, but the result depends on SDK behavior and time. This is useful but not enough.
- `EnsureConfigured` failure behavior for upload/metadata/read/delete methods.

What is not cleanly testable now without real R2:

- `SaveDirectUploadAsync`
- `GetObjectAsync`, especially 404-to-null behavior
- `ReadPrefixAsync`
- `DeleteAsync` for single objects
- `DeleteAsync` for HLS manifest keys, including list pagination and prefix deletion

Those methods call a concrete `AmazonS3Client` created inside the service. There is no injected S3 interface or factory.

### Can The AWS / S3-Compatible Client Be Faked Cleanly?

Not cleanly with the current production shape.

The AWS SDK has interfaces such as `IAmazonS3`, but `R2VideoStorageService` stores and constructs `AmazonS3Client` directly. A test cannot replace the client without reflection or a local S3-compatible server. Both are poor choices for this repo's deterministic coverage goal.

Do not use real R2. Do not require MinIO or LocalStack for these unit/component tests unless a later integration-only task explicitly chooses that cost.

### Is There Already An Abstraction Around Video Storage?

Yes.

`IVideoObjectStorage` is the useful application seam:

- `StorageType`
- `BuildPlaybackUrl`
- `CreateUploadTargetAsync`
- `SaveDirectUploadAsync`
- `GetObjectAsync`
- `ReadPrefixAsync`
- `DeleteAsync`

This is enough to test command handlers, HLS publisher behavior, cleanup service behavior, and storage selector behavior with deterministic fakes. It is not enough to test `R2VideoStorageService` internals, because that class is the concrete implementation behind the abstraction.

### Are Presigned URL Generation And Object Upload/Delete Testable Without Real Network?

Presigned URL generation: partly yes.

- AWS signing can generate a URL locally.
- Current code can be given fake credentials, bucket, endpoint, public URL, and optional browser endpoint.
- The test should assert stable structural properties, not exact volatile signatures or expiration timestamps.
- A deterministic fake signer would still be cleaner if a seam is introduced.

Object upload/delete/read: no, not cleanly.

- Current implementation calls concrete AWS client methods.
- Tests would need real network, a local S3-compatible server, reflection, or production seam changes.
- The strict recommendation is a small seam rather than network.

### Would Adding Tests Require Production Code Changes?

No for broad handler/storage-lifecycle behavior.

The following can be tested now with no production code changes:

- `WorkVideoStorageSelector` with fake storages and fake environment.
- `WorkVideoPlaybackUrlBuilder` with fake storages.
- `IssueWorkVideoUploadCommandHandler`, `UploadLocalWorkVideoCommandHandler`, `ConfirmWorkVideoUploadCommandHandler`, `StartWorkVideoHlsJobCommandHandler`, `DeleteWorkVideoCommandHandler`, and cleanup service behavior using fake `IVideoObjectStorage` and existing store implementations.
- `WorkVideoHlsOutputPublisher` with a fake `IVideoObjectStorage`.
- `FfmpegVideoTranscoder` process-failure behavior with temp shell scripts.
- Local storage edge behavior with temp directories.

Yes for clean direct coverage of `R2VideoStorageService` object operations.

### Smallest Safe Testability Seam

Add one narrow factory around S3 client creation. Keep it internal to infrastructure if possible.

Recommended shape:

- Introduce an interface such as `IR2S3ClientFactory` in infrastructure.
- Factory methods return `IAmazonS3` for:
  - normal endpoint client
  - browser endpoint signing client
- Default implementation constructs `AmazonS3Client` exactly as today.
- `R2VideoStorageService` depends on the factory and caches the normal client.

Why this seam is preferable:

- It preserves `IVideoObjectStorage` as the application abstraction.
- It avoids real R2, MinIO, LocalStack, and reflection.
- It allows deterministic fakes for requests, 404 exceptions, read streams, paginated listings, delete calls, and presigned URL output.
- It keeps the production behavior unchanged except dependency construction.

Alternative acceptable seam:

- Inject `IAmazonS3` and a separate signer client provider directly into `R2VideoStorageService`.
- This is slightly less clean because the service has two endpoint concerns: server-side object operations and browser endpoint signing.

## 5. Prioritized Implementation Batches

### Batch 1: No Production-Code-Change Tests

Focus: highest value, deterministic, no seams.

Recommended tests:

- `WorkVideoStorageSelector`
  - Testing/development defaults to local when `ForceEnabledInDevelopment` is false.
  - Forced development mode selects R2 only when R2 playback URL is available.
  - Production-like environment falls back to local when R2 storage is missing or unconfigured.
  - `TryGetStorage` is case-insensitive.
- `WorkVideoPlaybackUrlBuilder`
  - YouTube returns null.
  - Local/R2 direct source returns storage playback URL.
  - HLS valid source key resolves through embedded storage type.
  - HLS malformed source key returns null.
  - Missing storage returns null.
  - Timeline preview object URLs use `BuildStorageObjectUrl` through fake storage.
- Command-handler failure side effects with fake storage:
  - Issue upload: missing storage and storage target creation throw.
  - Confirm upload: object missing, R2 MIME mismatch, size mismatch, invalid MP4 prefix, storage unavailable.
  - Upload local: non-local session, missing session, storage unavailable, storage save throws.
  - Start HLS job: invalid MP4, transcoder returns error, publisher throws.
  - Assert no `WorkVideo` row and no version increment on failures.
- `WorkVideoHlsOutputPublisher`
  - Manifest/VTT/JPG/segment content types.
  - Files are published under the HLS prefix.
  - Publish stops on storage failure and exposes partial-publish risk.
- `WorkVideoCleanupStore`
  - YouTube no-op.
  - HLS source key parses to underlying storage type and manifest key.
  - Malformed HLS source key no-op.
  - Duplicate pending cleanup is not inserted.

Recommended level: mostly Component, with a few Unit tests for URL builder/selector.

### Batch 2: Minimal Seam Tests If Needed

Focus: direct `R2VideoStorageService` behavior after adding the smallest S3 client factory seam.

Recommended tests:

- Unconfigured methods throw `InvalidOperationException`; `BuildPlaybackUrl` returns null.
- Configured `BuildPlaybackUrl` trims public URL slash and appends storage key.
- Presigned upload target:
  - Uses `PUT`.
  - Uses expected bucket/key/content type.
  - Uses browser endpoint when configured.
  - Does not call network.
- `SaveDirectUploadAsync` sends bucket/key/content type, stream, and disables chunk encoding.
- `GetObjectAsync` maps S3 404 to null and returns content type/length for success.
- `ReadPrefixAsync` requests byte range `0..length-1` and returns only bytes read.
- `DeleteAsync` deletes one object for non-manifest keys.
- `DeleteAsync` for manifest keys lists the HLS prefix and deletes every object across paginated results.
- AWS exceptions other than 404 are not swallowed.

Recommended level: Component or unit-style Infrastructure tests with fake `IAmazonS3`.

### Batch 3: HLS / FFmpeg Failure Tests

Focus: process and publish failures, not coverage percentages.

Recommended tests:

- `FfmpegVideoTranscoder`
  - Missing ffmpeg executable returns "Unable to start HLS processing...".
  - Non-zero ffmpeg with stderr includes stderr in error.
  - Non-zero ffmpeg without stderr returns generic processing error.
  - Timeout returns timeout error and kills the process.
  - Successful ffmpeg without manifest returns missing-manifest error.
  - ffprobe missing/non-zero/invalid duration still returns HLS success but omits preview artifacts.
  - Preview ffmpeg failure omits VTT/sprite and does not fail HLS.
  - One-frame preview VTT boundary is valid.
- `StartWorkVideoHlsJobCommandHandler`
  - Transcoder error returns bad request and does not save DB state.
  - Publisher failure returns failure and does not add `WorkVideo`.
  - No-preview success adds HLS video without timeline preview keys.

Recommended level: Component with fake shell tools/temp directories; avoid real ffmpeg dependency.

### Batch 4: Cleanup / Idempotency Tests

Focus: durable side effects after delete/expiry and partial failures.

Recommended tests:

- `WorkVideoService.ProcessCleanupJobsAsync`
  - No pending jobs returns 0 and does not save.
  - Transient delete failure below attempt 5 remains `Pending`.
  - Failure at attempt 5 marks `Failed`.
  - Success after previous attempts marks `Succeeded` and clears `LastError`.
  - Only 20 jobs are processed per batch.
- `WorkVideoCleanupStore`
  - Duplicate pending job is not inserted.
  - Succeeded or failed existing job does not block a new pending cleanup if that is desired; if not desired, document the invariant.
- Storage cleanup:
  - Local missing file delete is idempotent.
  - Local manifest delete removes HLS directory; missing directory is no-op.
  - R2 manifest delete removes all objects under prefix after the S3 seam exists.
- Endpoint/command delete:
  - YouTube delete does not create cleanup job.
  - HLS delete with malformed source key is handled explicitly or documented as no cleanup.

Recommended level: Component with fake storage and temp directories; R2-specific deletion after Batch 2 seam.

## Final Recommendation

Start with Batch 1. It will cover the highest-risk DB and side-effect behavior without production changes and without real R2.

Only add the S3 factory seam in Batch 2 if direct `R2VideoStorageService` request/response behavior is required. That seam is justified because current R2 coverage is 9.0% line / 0.0% branch and object operations cannot be cleanly faked today.

The strict acceptance bar for follow-up tests should be side-effect based:

- failed upload/confirm/HLS paths do not create `WorkVideo` rows
- failed paths do not increment `VideosVersion`
- partial HLS publish behavior is either cleaned up or explicitly documented as a known risk
- cleanup jobs are scheduled exactly when storage objects can outlive database rows
- R2 tests never touch real Cloudflare R2

## 6. Batch 1 Implementation Update - 2026-04-26

### Summary

Batch 1 no-production-code-change tests were added for deterministic WorkVideo storage selection, playback URL resolution, local filesystem behavior, HLS path planning/workspace/publisher behavior, and cleanup enqueue idempotency.

No production code was modified. No S3 client factory seam was added. No real R2, S3, Cloudflare, credentials, ffmpeg, or external network services were used.

### Files Changed

- `backend/tests/WoongBlog.Api.ComponentTests/WorkVideoComponentTests.cs`
- `backend/tests/WoongBlog.Api.UnitTests/WorkVideoHlsJobPlanTests.cs`
- `todolist-2026-04-26.md`
- `backend/reports/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26.md`
- `backend/reports/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26.json`
- `backend/reports/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26.html`
- `backend/reports/backend-workvideo-r2-hls-failure-coverage-audit-2026-04-26/batch1-prechange-backup/*`

### Tests Added

Component tests:

- `WorkVideoStorageSelector_UsesLocalInTestingUnlessR2IsForced`
- `WorkVideoStorageSelector_UsesR2WhenForcedInTestingAndPlaybackIsAvailable`
- `WorkVideoStorageSelector_FallsBackToLocalWhenR2IsMissingOrUnavailable`
- `WorkVideoStorageSelector_TryGetStorage_IsCaseInsensitive`
- `WorkVideoPlaybackUrlBuilder_ResolvesDirectAndHlsStorageUrls`
- `WorkVideoPlaybackUrlBuilder_ReturnsNullForUnsupportedSources`
- `WorkVideoPlaybackUrlBuilder_BuildStorageObjectUrl_UsesRequestedStorage`
- `LocalVideoStorageService_SaveOverwritesExistingFileAndDeleteIsIdempotent`
- `LocalVideoStorageService_MissingObjectOperationsAreSafe`
- `WorkVideoHlsWorkspace_CreatesSeparatedSourceAndOutputPathsAndCleansUpLease`
- `WorkVideoHlsOutputPublisher_StoresArtifactsWithExpectedContentTypesAndKeys`
- `WorkVideoHlsOutputPublisher_StopsOnStorageFailureAndLeavesPreviouslySavedArtifactsVisible`
- `WorkVideoCleanupStore_EnqueuesHlsManifestCleanupOnceForUnderlyingStorage`
- `WorkVideoCleanupStore_SkipsYouTubeAndMalformedHlsSourceKeys`

Unit test:

- `Create_WithR2Storage_EmbedsUnderlyingStorageInHlsSourceKey`

### Behavior Covered

- Testing/development storage selection defaults to local unless R2 is explicitly forced.
- Forced testing mode selects R2 only when the R2 storage can produce a playback URL.
- Production-like environments fall back to local when R2 storage is missing or unavailable.
- Storage lookup is case-insensitive.
- Playback URL resolution covers direct local/R2 sources, valid HLS source keys, timeline preview object URLs, YouTube no-URL behavior, malformed HLS source keys, missing storage, and trailing-slash normalization through deterministic fake storage.
- Local storage overwrites existing files through the current `File.Create` behavior and supports idempotent delete calls.
- Missing local object reads and deletes are safe under current behavior.
- HLS workspace source and output paths are separated and the workspace lease cleans up its temp directory.
- HLS publisher stores manifest, segment, VTT, and JPG artifacts with expected storage keys and content types.
- HLS publisher stops on storage failure and leaves already-published artifacts visible, documenting the current partial-publish risk.
- Cleanup store skips YouTube cleanup, skips malformed HLS source keys, parses HLS source keys to the underlying storage type and manifest key, and suppresses duplicate pending cleanup once the first job has been persisted.
- R2-backed HLS job planning embeds the underlying R2 storage type in the HLS source key without touching R2.

### Intentionally Not Changed

- No production code.
- No `R2VideoStorageService` object-operation tests that would require a seam.
- No S3/R2 client factory.
- No real R2, S3, Cloudflare, credentials, external network, or local S3-compatible service.
- No Auth, Proxy, Security validator, or unrelated coverage work.
- No ffmpeg process tests. `FfmpegVideoTranscoder` coverage is unchanged in this batch.
- No path traversal root-boundary assertion for `LocalVideoStorageService`; current production behavior relies on application-generated storage keys and does not enforce a root-boundary check inside the local storage service.

### Validation Results

| Command | Result |
| --- | --- |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj --filter WorkVideoHlsJobPlan` | Passed, 3 tests |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj --filter WorkVideoComponentTests` | Passed, 25 tests |
| `dotnet test backend/tests/WoongBlog.Api.UnitTests/WoongBlog.Api.UnitTests.csproj` | Passed, 56 tests |
| `dotnet test backend/tests/WoongBlog.Api.ComponentTests/WoongBlog.Api.ComponentTests.csproj` | Passed, 113 tests |
| `./scripts/run-backend-coverage.sh component -v minimal` | Passed, 113 component tests |
| `./scripts/run-backend-coverage.sh full -v minimal` | Passed; contract skipped, 113 component, 56 unit, 35 architecture, 194 integration tests |
| `dotnet test backend/WoongBlog.sln` | Passed; contract skipped, 113 component, 56 unit, 35 architecture, 194 integration tests |
| `git diff --check` | Passed |

Existing warning observed during test runs: `NU1901` for `AWSSDK.Core` 4.0.0.17 low-severity vulnerability.

### Coverage Changes

Baseline values are from this audit's original full coverage snapshot. Current values are from regenerated `coverage/backend/full/report/Summary.json`.

| Target | Baseline line | Baseline branch | Current full line | Current full branch | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| Full backend | 92.2% | 70.5% | 92.5% | 72.6% | Improved after Batch 1 tests. |
| `WorkVideoStorageSelector` | 78.2% | 30.0% | 100.0% | 90.0% | Covers local default, forced R2, unavailable R2 fallback, and case-insensitive lookup. |
| `WorkVideoPlaybackUrlBuilder` | 100.0% | 66.6% | 100.0% | 100.0% | Branch-complete for supported, missing, YouTube, and malformed HLS cases. |
| `LocalVideoStorageService` | 91.4% | 70.0% | 100.0% | 90.0% | Covers overwrite, missing object reads/deletes, idempotent deletes, and HLS directory delete behavior. |
| `FfmpegVideoTranscoder` | 61.7% | 50.0% | 61.7% | 50.0% | Unchanged; process failure tests remain outside this no-ffmpeg batch. |
| `WorkVideoHlsJobPlan` | 100.0% | 100.0% | 100.0% | 100.0% | Added R2 source-key planning characterization. |
| `WorkVideoCleanupStore` | 100.0% | n/a | 100.0% | n/a | Added direct enqueue/no-op/idempotency behavior tests. |

Component-only regenerated coverage highlights:

| Target | Component line | Component branch |
| --- | ---: | ---: |
| Component suite | 44.1% | 37.8% |
| `WorkVideoStorageSelector` | 100.0% | 90.0% |
| `WorkVideoPlaybackUrlBuilder` | 100.0% | 100.0% |
| `LocalVideoStorageService` | 91.4% | 90.0% |
| `FfmpegVideoTranscoder` | 0.0% | 0.0% |

`WorkVideoHlsOutputPublisher` and `WorkVideoHlsWorkspace` were exercised by component tests, but they are not listed as separate classes in the current ReportGenerator `Summary.json`; `WorkVideoHlsWorkspaceLease` is listed and is now covered at 100.0% line / 100.0% branch.

### Risks And Yellow Flags

- `WorkVideoHlsOutputPublisher` still has the current partial-publish behavior: if a later artifact upload fails, earlier artifacts remain stored and no cleanup is scheduled by the publisher itself.
- `WorkVideoCleanupStore` duplicate suppression is verified for already-persisted pending jobs. In-memory duplicate enqueue before a save remains a current behavior nuance.
- `LocalVideoStorageService` still does not enforce a root boundary for arbitrary storage keys. The tested paths are application-shaped keys generated by existing WorkVideo flows.
- Direct R2 object behavior remains intentionally untested because `R2VideoStorageService` constructs a concrete AWS S3 client internally.

### Remaining Batch 2 Items Requiring An S3/R2 Seam

These remain blocked until a narrow S3/R2 client factory or equivalent seam is added:

- `R2VideoStorageService.SaveDirectUploadAsync` request shape and failure propagation.
- `R2VideoStorageService.GetObjectAsync` success and 404-to-null mapping.
- `R2VideoStorageService.ReadPrefixAsync` byte-range behavior.
- `R2VideoStorageService.DeleteAsync` single-object delete behavior.
- `R2VideoStorageService.DeleteAsync` HLS manifest-prefix deletion with paginated listings.
- AWS exceptions other than 404 are not swallowed.
- Deterministic presigned upload target request capture, including browser endpoint signing, without relying on a concrete SDK client.

### Final Recommendation

Batch 1 is complete for the no-production-code-change storage/path/url/cleanup scope. The next useful step is Batch 2: add the smallest S3/R2 client factory seam, then test `R2VideoStorageService` object operations with deterministic fakes and no real network.
