# Video Preview Recording Audit

- Date: 2026-04-24
- Scope: add and execute a Playwright recording spec for long-video hover preview behavior, save `.webm` output

## Changed
- Added [video-preview-recording-0424.spec.ts](/mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/video-preview-recording-0424.spec.ts).
- The spec generates or reuses a long MP4 fixture, navigates to a long-video work, hovers the progress-region overlay, and records browser video output.

## Outcome
- The recording spec was executed.
- The hover preview bubble did not become visible during the live browser run, so the spec remained red.
- The `.webm` recording artifact was still captured and copied for inspection.

## Artifacts
- Stable copied recording:
  - [work-video-hover-preview-long.webm](</mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/playwright/0424test/video-preview/work-video-hover-preview-long.webm>)
- Raw Playwright run output:
  - [video.webm](</mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/test-results/playwright/video-preview-recording-04-64aed-er-preview-after-processing-chromium-public/video.webm>)
  - [trace.zip](</mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/test-results/playwright/video-preview-recording-04-64aed-er-preview-after-processing-chromium-public/trace.zip>)
  - [test-failed-1.png](</mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/test-results/playwright/video-preview-recording-04-64aed-er-preview-after-processing-chromium-public/test-failed-1.png>)

## Validation
- `PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 npx playwright test tests/video-preview-recording-0424.spec.ts --workers=1`
  - Result: fail
  - Failure: `work-video-timeline-preview` did not become visible in the live browser session

## Interpretation
- This confirms the current system can record the scenario end-to-end and save a `.webm`.
- It does not yet confirm that the preview bubble is rendering successfully in a real browser session for that long-video path.
- The most likely next debugging targets are the live overlay hit area, preview cue loading timing, or preview asset fetch/availability in the browser runtime.

## Recommendation
- Inspect the saved `.webm` and trace first.
- If you want, the next pass should be a dedicated debug pass on the live browser path until the recording spec goes green.
