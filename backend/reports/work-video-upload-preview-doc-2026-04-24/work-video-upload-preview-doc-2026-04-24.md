# Work Video Upload Preview Documentation Audit

## Summary
- Added a new documentation file describing the full frontend-to-backend flow for uploaded work videos.
- Documented how admin upload triggers HLS processing, how preview sprite/VTT assets are generated and published, and how the public player renders YouTube-like hover previews.
- Documented the current implementation constraints, including custom overlay hover capture and short-video preview behavior.

## Intentionally Not Changed
- No production code was changed in this pass.
- No tests were modified in this pass.
- No historical video rows were reprocessed.

## Goal Check
- Goal: explain how uploaded videos become registered work videos. Met.
- Goal: explain how progress hover preview works. Met.
- Goal: base the explanation on actual current code paths. Met.

## Validations Performed
- Source trace across:
  - `src/components/admin/WorkEditor.tsx`
  - `src/components/content/WorkVideoPlayer.tsx`
  - `src/lib/api/works.ts`
  - `backend/src/WoongBlog.Application/Modules/Content/Works/WorkVideos/StartWorkVideoHlsJobCommandHandler.cs`
  - `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/FfmpegVideoTranscoder.cs`
  - `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/WorkVideoHlsOutputPublisher.cs`
  - `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkQueryStore.cs`
  - `backend/src/WoongBlog.Infrastructure/Modules/Content/Works/Persistence/WorkVideoQueryStore.cs`
- Documentation artifact written to `docs/work-video-upload-preview-flow-2026-04-24.md`

## Risks / Yellow Flags
- This document reflects the codebase as of 2026-04-24. If upload endpoints, preview asset generation, or player controls change later, the document can drift.
- The explanation intentionally describes the current custom overlay implementation, not browser-native controls behavior.

## Final Recommendation
- Keep this file as the current implementation note.
- If the preview pipeline is later reworked again, update this document in the same change set.
