# Work Video Upload TODO

## Schema / Backend
- [x] Add `Work.VideosVersion`
- [x] Add `WorkVideo`
- [x] Add `WorkVideoUploadSession`
- [x] Add `VideoStorageCleanupJob`
- [x] Add `SchemaPatch`
- [x] Add schema patch runner and `20260410_work_videos` patch
- [x] Add work video DTOs and service interfaces
- [x] Add local object storage implementation
- [x] Add R2 object storage implementation
- [x] Add playback URL builder
- [x] Add cleanup hosted worker
- [x] Add work video endpoints
- [x] Add work delete cleanup hook
- [x] Extend admin/public work read models with `videos[]`

## Frontend
- [x] Add strict `videos[]` parser in `src/lib/api/works.ts`
- [x] Add `WorkVideoPlayer`
- [x] Add staged video drafts to create flow
- [x] Add create-and-add-videos submit flow
- [x] Add edit-time add/upload/reorder/remove video controls
- [x] Render work videos on public work detail

## Tests
- [x] Update `src/test/work-editor.test.tsx`
- [x] Add `src/test/work-video-player.test.tsx`
- [x] Update `src/test/public-api-clients.test.ts`
- [x] Update `src/test/public-api-contracts.test.ts`
- [x] Add backend lifecycle tests
- [ ] Add backend cleanup worker tests
- [ ] Add backend S3-compatible storage tests
- [x] Add Playwright create flow coverage
- [x] Add Playwright edit flow coverage
- [x] Add Playwright public work video coverage
- [ ] Add Playwright S3-compatible lane

## Verification
- [x] Run targeted Vitest
- [x] Run targeted backend tests
- [x] Run targeted Playwright stack tests
- [ ] Run S3-compatible regression
- [x] Run real R2 smoke or manual verification
- [x] Re-close real browser R2 upload flow after CORS/update verification

## Deferred
- [ ] resumable upload
- [ ] duration extraction
- [ ] automatic thumbnail extraction
- [ ] Cloudflare Stream
