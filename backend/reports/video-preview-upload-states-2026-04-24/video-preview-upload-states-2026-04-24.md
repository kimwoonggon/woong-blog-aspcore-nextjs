# Video Preview / Upload States Audit

- Date: 2026-04-24
- Scope: works video hover preview architecture, backend sprite/VTT generation, admin upload stage messaging, autoplay rollback

## Changed
- Removed default autoplay and restored user-driven playback in [WorkVideoPlayer.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/content/WorkVideoPlayer.tsx) and [works/[slug]/page.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/app/(public)/works/[slug]/page.tsx).
- Kept desktop size modes while moving preview hover to a transparent overlay aligned with the native progress region instead of rendering a separate visible bar.
- Upgraded backend preview generation in [FfmpegVideoTranscoder.cs](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/FfmpegVideoTranscoder.cs) to generate time-indexed sprite/VTT data instead of a single preview frame plus one long cue.
- Added preview-generation options in [WorkVideoHlsOptions.cs](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/backend/src/WoongBlog.Infrastructure/Modules/Content/Works/WorkVideos/WorkVideoHlsOptions.cs).
- Added admin works stage messaging in [WorkEditor.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/admin/WorkEditor.tsx) for HLS uploads: `업로드 중...`, `처리 중...`, `준비 완료`.
- Extended tests for:
  - notion 1-second autosave and `Ctrl+S`
  - work video hover preview and desktop sizing
  - work editor upload status copy
  - backend HLS preview asset generation and preview URL mapping

## Not Changed
- The native browser video controls were not replaced; preview still relies on an overlay positioned over the progress area because the native bar itself does not expose hover coordinates to React.
- The e2e preview assertion remains environment-dependent and skips when the uploaded fixture does not yield preview assets in that runtime.

## Goal Check
- Autoplay removed: yes.
- Hover preview moved toward the native progress region without rendering an extra bar: yes.
- Backend preview assets are now time-indexed in code and covered by backend tests: yes.
- Works upload status copy now reflects upload/processing/ready phases: yes.

## Validation
- `npx vitest run src/test/work-editor.test.tsx src/test/work-video-player.test.tsx src/test/blog-notion-workspace.test.tsx src/test/auth-csrf.test.ts`
  - Result: pass, 41 tests
- `npm run typecheck`
  - Result: pass
- `npm run lint`
  - Result: pass with warnings only
- `dotnet test backend/tests/WoongBlog.Api.IntegrationTests/WoongBlog.Api.IntegrationTests.csproj --filter WorkVideoEndpointsTests`
  - Result: pass, 7 tests
- `dotnet test backend/WoongBlog.sln`
  - Result: pass
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/ui-admin-notion-autosave-info.spec.ts tests/public-work-videos.spec.ts --workers=1`
  - Result: pass, 8 passed and 1 skipped

## Risks / Follow-up
- The overlay approach approximates native progress hover by matching the control region, but it is still not true native progress-bar event capture.
- Preview fidelity now depends on FFmpeg/FFprobe availability and successful sprite/VTT generation during HLS processing.
- The admin upload stage messaging is phase-based UX, not byte-accurate upload telemetry.

## Recommendation
- Keep this pass.
- If you want truly strong preview fidelity, the next step is improving the preview generation density and validating it against longer real-world videos in a non-skipped browser test path.
