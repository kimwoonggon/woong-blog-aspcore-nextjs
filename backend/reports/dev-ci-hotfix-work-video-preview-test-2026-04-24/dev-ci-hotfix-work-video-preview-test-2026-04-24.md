# Dev CI Hotfix Audit

## Summary
- Investigated failed `CI Dev` run `24891406974` on `dev`.
- Confirmed the failure was in the `Frontend lint, types, and unit tests` job, specifically `src/test/work-video-player.test.tsx`.
- Fixed the mismatch by making the test derive its expected preview size from the implementation's `timelinePreviewDisplayScale` instead of stale hard-coded dimensions.
- Exported the preview scale constant from `WorkVideoPlayer` so the unit test stays aligned with the component behavior.

## Intentionally Not Changed
- Did not change actual hover preview behavior.
- Did not alter backend video processing or any Playwright specs.
- Did not commit local Playwright recording artifacts or backup directories.

## Goal Check
- Goal: identify why `dev` CI failed. Met.
- Goal: fix the concrete failing test. Met.
- Goal: prepare a safe follow-up push to re-run `dev` CI. Met.

## Validations Performed
- GitHub Actions failed-log inspection via `gh run view 24891406974 --log-failed`
- `npx vitest run src/test/work-video-player.test.tsx`
- `npm run typecheck`
- `npm run lint` (warnings only, no errors)

## Risks / Yellow Flags
- A separate local full-suite run exposed an unrelated `src/test/work-detail-metadata.test.ts` issue that was not the cause of the `dev` CI failure inspected here.
- Remaining local `git status` noise is still dominated by Playwright `.webm` artifacts and backup directories.

## Final Recommendation
- Push this hotfix and let `dev` CI re-run.
- If CI still fails, inspect whether the unrelated metadata test is reproducible in GitHub Actions or only local.
