# Origin/Dev vs Current Frontend Runtime Performance

Generated: 2026-04-22T17:18:09.171Z
Completed: 2026-04-22T17:28:10.302Z

## Environment

- Base URL: `http://127.0.0.1:32080`
- Browser: Chromium, one worker
- Baseline: `origin/dev` (0318861952a9ec2ab58150be65f9a74492601f65)
- Current: `feat/phase-2-backend-discompostion` (246d8577180df4b1a457a7f973a9dcbdfc395471)
- Iterations: 1 warmup + 3 measured for route/API/interaction; 1 warmup + 3 measured for mutations
- Host: DESKTOP-PVN76TL, linux x64, Node v22.22.0
- Docker: Docker version 29.2.1, build a5c7197

## Classification Rules

- Improved: current median at least 10% faster and p95 not worse.
- Neutral: current remains within the 10% regression threshold.
- Regression: current median or p95 is more than 10% slower, or baseline passes and current fails.
- Correctness-only improvement: baseline fails but current passes.

## Public cache/revalidation

Recommendation: Investigate before release: 10 regression(s) detected.

| Scenario | Metric | Baseline median / p95 | Current median / p95 | Delta median | Delta p95 | Classification | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| Home cold first load to primary content visible | time-to-primary-content-ms | 208.74 ms / 208.74 ms | 160.03 ms / 160.03 ms | -23.34% | -23.34% | Improved |  |
| Home warm load to primary content visible | time-to-primary-content-ms | 131.13 ms / 160.55 ms | 159.71 ms / 202.90 ms | +21.80% | +26.38% | Regression |  |
| Home warm browser navigation | browser-navigation-duration-ms | 115.80 ms / 140.28 ms | 145.90 ms / 167.41 ms | +25.99% | +19.34% | Regression |  |
| Home warm route TTFB | route-ttfb-ms | 28.30 ms / 36.67 ms | 20.40 ms / 47.49 ms | -27.92% | +29.51% | Regression |  |
| Study list cold first load to primary content visible | time-to-primary-content-ms | 198.48 ms / 198.48 ms | 386.17 ms / 386.17 ms | +94.56% | +94.56% | Regression |  |
| Study list warm load to primary content visible | time-to-primary-content-ms | 138.61 ms / 150.94 ms | 130.58 ms / 140.84 ms | -5.79% | -6.69% | Neutral |  |
| Study list warm browser navigation | browser-navigation-duration-ms | 120.50 ms / 123.11 ms | 99.20 ms / 102.89 ms | -17.68% | -16.42% | Improved |  |
| Study list warm route TTFB | route-ttfb-ms | 22.20 ms / 24.00 ms | 16.70 ms / 18.05 ms | -24.77% | -24.79% | Improved |  |
| Works list cold first load to primary content visible | time-to-primary-content-ms | 164.98 ms / 164.98 ms | 438.06 ms / 438.06 ms | +165.52% | +165.52% | Regression |  |
| Works list warm load to primary content visible | time-to-primary-content-ms | 152.85 ms / 174.23 ms | 135.16 ms / 154.40 ms | -11.57% | -11.38% | Improved |  |
| Works list warm browser navigation | browser-navigation-duration-ms | 128.30 ms / 138.02 ms | 109.50 ms / 109.59 ms | -14.65% | -20.60% | Improved |  |
| Works list warm route TTFB | route-ttfb-ms | 23.10 ms / 24.63 ms | 21.40 ms / 23.20 ms | -7.36% | -5.81% | Neutral |  |
| Contact cold first load to primary content visible | time-to-primary-content-ms | 195.99 ms / 195.99 ms | 178.01 ms / 178.01 ms | -9.17% | -9.17% | Neutral |  |
| Contact warm load to primary content visible | time-to-primary-content-ms | 118.93 ms / 119.18 ms | 102.65 ms / 103.45 ms | -13.69% | -13.20% | Improved |  |
| Contact warm browser navigation | browser-navigation-duration-ms | 116.90 ms / 116.90 ms | 100.80 ms / 101.70 ms | -13.77% | -13.00% | Improved |  |
| Contact warm route TTFB | route-ttfb-ms | 18.50 ms / 20.57 ms | 12.10 ms / 12.28 ms | -34.59% | -40.30% | Improved |  |
| Introduction cold first load to primary content visible | time-to-primary-content-ms | 113.15 ms / 113.15 ms | 104.91 ms / 104.91 ms | -7.28% | -7.28% | Neutral |  |
| Introduction warm load to primary content visible | time-to-primary-content-ms | 96.83 ms / 99.39 ms | 103.07 ms / 107.76 ms | +6.44% | +8.42% | Neutral |  |
| Introduction warm browser navigation | browser-navigation-duration-ms | 102.20 ms / 104.72 ms | 100.50 ms / 105.54 ms | -1.66% | +0.78% | Neutral |  |
| Introduction warm route TTFB | route-ttfb-ms | 15.80 ms / 15.98 ms | 11.50 ms / 11.77 ms | -27.22% | -26.35% | Improved |  |
| Public API home | api-response-duration-ms | 8.06 ms / 9.08 ms | 8.62 ms / 10.07 ms | +6.95% | +10.90% | Regression |  |
| Public API blogs | api-response-duration-ms | 5.43 ms / 6.41 ms | 5.65 ms / 6.13 ms | +4.05% | -4.37% | Neutral |  |
| Public API works | api-response-duration-ms | 5.61 ms / 5.93 ms | 6.46 ms / 9.22 ms | +15.15% | +55.48% | Regression |  |
| Public API contact page | api-response-duration-ms | 2.93 ms / 3.06 ms | 4.11 ms / 4.84 ms | +40.27% | +58.17% | Regression |  |
| Public API introduction page | api-response-duration-ms | 2.92 ms / 3.03 ms | 4.35 ms / 4.46 ms | +48.97% | +47.19% | Regression |  |
| Public API site settings | api-response-duration-ms | 3.11 ms / 4.01 ms | 3.94 ms / 4.70 ms | +26.69% | +17.21% | Regression |  |
| Admin blog save to public route refreshed and visible | mutation-to-public-visible-ms | 30403.77 ms / 30480.32 ms | 288.17 ms / 306.25 ms | -99.05% | -99.00% | Improved |  |
| Admin work save to public route refreshed and visible | mutation-to-public-visible-ms | 30405.60 ms / 30407.24 ms | 297.97 ms / 307.04 ms | -99.02% | -98.99% | Improved |  |

## Resume PDF SSR

Recommendation: Investigate before release: 1 regression(s) detected.

| Scenario | Metric | Baseline median / p95 | Current median / p95 | Delta median | Delta p95 | Classification | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| Resume cold first load to primary content visible | time-to-primary-content-ms | 181.42 ms / 181.42 ms | 126.37 ms / 126.37 ms | -30.34% | -30.34% | Improved |  |
| Resume warm load to primary content visible | time-to-primary-content-ms | 105.66 ms / 109.07 ms | 101.89 ms / 117.13 ms | -3.57% | +7.39% | Neutral |  |
| Resume warm browser navigation | browser-navigation-duration-ms | 103.80 ms / 106.77 ms | 90.10 ms / 106.03 ms | -13.20% | -0.69% | Improved |  |
| Resume warm route TTFB | route-ttfb-ms | 16.40 ms / 18.56 ms | 10.40 ms / 14.18 ms | -36.59% | -23.60% | Improved |  |
| Public API resume | api-response-duration-ms | 3.48 ms / 4.23 ms | 4.38 ms / 6.07 ms | +25.86% | +43.50% | Regression |  |
| Admin resume upload to public resume visible | mutation-to-public-visible-ms | 30149.82 ms / 30151.55 ms | 481.88 ms / 490.51 ms | -98.40% | -98.37% | Improved |  |

## Study/Works pagination

Recommendation: Investigate before release: 2 regression(s) detected.

| Scenario | Metric | Baseline median / p95 | Current median / p95 | Delta median | Delta p95 | Classification | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| Study Next click to page 2 grid visible | interaction-to-ready-ms | 149.10 ms / 150.61 ms | 136.58 ms / 141.38 ms | -8.40% | -6.13% | Neutral |  |
| Study Previous click to page 1 grid visible | interaction-to-ready-ms | 176.53 ms / 216.17 ms | 137.41 ms / 140.31 ms | -22.16% | -35.09% | Improved |  |
| Works Next click to page 2 grid visible | interaction-to-ready-ms | 151.19 ms / 156.45 ms | 141.78 ms / 160.18 ms | -6.22% | +2.38% | Neutral |  |
| Works Previous click to page 1 grid visible | interaction-to-ready-ms | 139.16 ms / 167.98 ms | 173.70 ms / 175.18 ms | +24.82% | +4.29% | Regression |  |
| Study direct page 2 load to grid visible | time-to-primary-content-ms | 147.03 ms / 153.07 ms | 141.72 ms / 204.24 ms | -3.61% | +33.43% | Regression |  |
| Works direct page 2 load to grid visible | time-to-primary-content-ms | 135.93 ms / 139.06 ms | 126.53 ms / 129.48 ms | -6.92% | -6.89% | Neutral |  |

## Admin editor UX

Recommendation: Investigate before release: 2 regression(s) detected.

| Scenario | Metric | Baseline median / p95 | Current median / p95 | Delta median | Delta p95 | Classification | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| Blog editor open to form ready | time-to-form-ready-ms | 413.99 ms / 430.39 ms | 389.22 ms / 411.32 ms | -5.98% | -4.43% | Neutral | baseline correctness: Only one Update Post action is present; top save action is absent.; current correctness: Only one Update Post action is present; top save action is absent. |
| Blog bottom save to saved/navigation state | mutation-to-navigation-ready-ms | 127.72 ms / 3707.22 ms | 144.42 ms / 149.58 ms | +13.08% | -95.97% | Regression |  |
| Blog keyboard save to saved/navigation state | mutation-to-navigation-ready-ms | 101.68 ms / 3694.79 ms | 122.38 ms / 134.63 ms | +20.36% | -96.36% | Regression |  |
| Work editor save to saved/navigation state | mutation-to-navigation-ready-ms | 135.86 ms / 3713.42 ms | 143.72 ms / 147.25 ms | +5.79% | -96.03% | Neutral |  |

## AI Fix dialog

Recommendation: Accept: no runtime regression detected in this area.

| Scenario | Metric | Baseline median / p95 | Current median / p95 | Delta median | Delta p95 | Classification | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| AI Fix click to runtime config and provider dropdown visible | interaction-to-provider-ready-ms | 107.61 ms / 114.68 ms | 102.26 ms / 104.29 ms | -4.97% | -9.06% | Neutral | baseline: Runtime providers: codex; current: Runtime providers: openai, codex; baseline correctness: Provider correctness issue: expected OPENAI and CODEX when OpenAI/Codex access is configured; got codex. |

## Candidate Notes

### Baseline
- Ref: `origin/dev`
- Commit: `0318861952a9ec2ab58150be65f9a74492601f65`
- Seed: `perf-baseline-abgprx`

### Current
- Ref: `current-working-tree`
- Commit: `246d8577180df4b1a457a7f973a9dcbdfc395471`
- Seed: `perf-current-abqy9c`

