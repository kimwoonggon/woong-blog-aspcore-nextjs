# Notion / Video Controls Audit

- Date: 2026-04-24
- Scope: notion autosave timing and shortcut save, work video seek/preview controls, desktop resize behavior

## Changed
- Switched notion autosave to a true idle debounce of about 1 second in [BlogNotionWorkspace.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/admin/BlogNotionWorkspace.tsx).
- Moved notion autosave scheduling off a React effect and into the editor change path so each new keystroke resets the timer instead of only the first dirty change starting the countdown.
- Added `Ctrl+S` / `Cmd+S` immediate save support in notion view, reusing the same document save path and triggering immediate revalidation.
- Rebuilt [WorkVideoPlayer.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/components/content/WorkVideoPlayer.tsx) to include:
  - custom seek bar below native controls
  - click-to-seek
  - hover preview bubble when preview sprite/VTT assets are available
  - desktop-only resize modes (`Fit`, `Wide`, `Theater`)
- Enabled desktop resize controls on public works detail usage in [works/[slug]/page.tsx](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/src/app/(public)/works/[slug]/page.tsx).
- Extended unit and browser tests for notion shortcut save and work video controls.

## Not Changed
- Backend preview generation was not expanded beyond the existing preview asset pipeline.
- Native browser control bars were not replaced; the custom seek bar is an additional layer under the video.
- Admin/editor reuse sites for `WorkVideoPlayer` keep the default sizing behavior; desktop resize was targeted at public works detail usage.

## Goal Check
- Notion view autosave near 1 second: yes.
- `Ctrl+S` immediate save in notion view: yes.
- Work video hover preview on a stable hover target: yes, via custom seek bar when preview assets exist.
- Work video desktop resize support: yes.

## Validation
- `npx vitest run src/test/blog-notion-workspace.test.tsx src/test/work-video-player.test.tsx src/test/auth-csrf.test.ts`
  - Result: pass, 24 tests
- `npm run typecheck`
  - Result: pass
- `npm run lint`
  - Result: pass with warnings only
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/ui-admin-notion-autosave-info.spec.ts tests/public-work-videos.spec.ts --workers=1`
  - Result: pass, 8 passed and 1 skipped

## Risks / Follow-up
- The existing backend preview asset generation still appears to be a coarse/single-frame pipeline in some environments, so the hover preview may not look like full time-indexed scrubbing everywhere.
- The new custom seek bar supplements native controls; if a full custom player is wanted later, it should be treated as a separate UX pass.
- `Ctrl+S` now saves immediately from notion view, which is correct for desktop authoring, but browser-level save interception should be watched if additional embedded editors are introduced.

## Recommendation
- Keep this pass.
- If richer scrub previews are needed next, upgrade the HLS preview asset generation pipeline to emit genuinely time-indexed sprite sheets and VTT cues.
