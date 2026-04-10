# Work Video Upload Implementation Plan

## Goal
- Add work video support with `videos[]` collections, create-with-videos authoring, YouTube plus direct upload, operational cleanup, and layered automated verification.

## Scope
- `WorkVideo`, `WorkVideoUploadSession`, `VideoStorageCleanupJob`, `SchemaPatches`
- local and R2-backed object storage
- create/edit work flows with video handling
- admin mutation endpoints for upload, confirm, youtube, reorder, delete
- public work detail rendering
- unit, integration, Playwright, S3-compatible, and real R2 smoke coverage

## Architecture
- `videos[]` on each work
- DB cascade for `Work -> WorkVideo` and `Work -> WorkVideoUploadSession`
- eventual external deletion through cleanup jobs
- `VideosVersion` conflict control for add/delete/reorder
- strict frontend parser for `videos[]`

## Security And Validation
- MP4 only
- `video/mp4` only
- max 200MB
- max 10 videos per work
- server-generated object keys only
- YouTube normalization limited to video URLs/IDs
- confirm step validates object existence and MP4 signature
- browser-based presigned uploads require bucket CORS on the R2 bucket

## Backend Implementation Steps
- [x] Add new entities and schema patch runner
- [x] Add storage services and playback URL builder
- [x] Add work video service and cleanup worker
- [x] Add admin video endpoints
- [x] Extend admin/public work read models
- [x] Ensure work delete enqueues video cleanup before cascade

## Frontend Implementation Steps
- [x] Extend `src/lib/api/works.ts` with `videos[]` parsing
- [x] Add `WorkVideoPlayer`
- [x] Add staged video support to `WorkEditor` create flow
- [x] Add immediate video mutations to `WorkEditor` edit flow
- [x] Render `videos[]` on public work detail

## Test Strategy
- [x] Vitest for editor state, player rendering, and parser strictness
- [x] xUnit for lifecycle, cleanup, and conflict control
- [x] Playwright for create-with-videos, edit, and public readback
- [ ] MinIO/S3-compatible regression lane
- [x] real R2 smoke/manual verification checklist

## Rollback / Recovery
- Keep work creation independent from video attachment
- Stop staged processing on first video failure and redirect to edit screen
- Retry object deletion in cleanup worker
- Keep schema patches idempotent

## Current External Gap
- Real R2 server-side smoke passes.
- Real browser upload to R2 passes after bucket CORS was corrected.
- Remaining external gap: MinIO-based S3-compatible regression lane.
