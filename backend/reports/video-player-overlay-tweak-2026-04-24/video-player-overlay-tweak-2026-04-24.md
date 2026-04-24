# Video Player Overlay Tweak Audit

## Summary
- Added a centered play overlay button that appears while the uploaded/HLS video is paused and disappears after playback starts.
- Reduced the displayed size of the timeline preview bubble by introducing a dedicated preview scale constant.
- Updated unit and Playwright coverage for the new overlay behavior and the adjusted preview rendering.

## Intentionally Not Changed
- Did not change autoplay policy.
- Did not change backend preview generation or DTO shape.
- Did not alter YouTube player behavior.

## Goal Check
- Goal: show a clear central play affordance while paused. Met.
- Goal: hide that affordance after playback starts. Met.
- Goal: make hover preview visually smaller. Met.

## Validations Performed
- `npx vitest run src/test/work-video-player.test.tsx`
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/public-work-videos.spec.ts --grep "PF-044" --workers=1`
- `npm run typecheck`
- `npm run lint` (warnings only in unrelated files)

## Risks / Yellow Flags
- The center overlay currently reappears whenever the video is paused, including mid-play pauses. That matches the current request, but if you want “only before first play” the state model would need to change.
- Preview sizing is now controlled by a single frontend scale constant. If sprite aspect or density changes later, that constant may need retuning.

## Final Recommendation
- If you want to tune preview size further, edit the preview scale constant in `src/components/content/WorkVideoPlayer.tsx` instead of hand-editing multiple inline styles.
