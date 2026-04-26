# Production-Like Full E2E Audit (production-like-full-e2e-2026-04-26)

## Scope
- Ran the full Playwright suite in a production-like Docker runtime.
- Runtime excluded `next dev`: current Docker production frontend, compose backend/db, and nginx routing.
- Requested output: total time and failure list.

## Environment
- Base URL: `http://woong-prod-full-e2e-nginx-1777130558`
- Workers: `1`
- Runner: `mcr.microsoft.com/playwright:v1.58.2-noble`
- Command class: `node scripts/run-e2e-latency.mjs -- --workers=1 --reporter=list,json`
- nginx health before run: `200 0.048422`

## Result
- Exit code: `1`
- Duration: 2h 15m 5s (8105s)
- Total tests: 589
- Passed: 248
- Failed: 209
- Skipped or did not run: 132
- Terminal breakdown: 14 skipped, 118 did not run, 248 passed.
- Latency artifacts: 466, latency budget failures 0, warnings 113.

## Counts By Project
| Project | Total | Failed |
| chromium-public | 301 | 15 |
| chromium-authenticated | 278 | 194 |
| chromium-runtime-auth | 10 | 0 |

## Failure Status Counts
- failed: 98
- timedOut: 111

## Failures By Category
- admin/editor/authenticated UI: 144
- works video/admin media: 24
- admin notion view: 21
- recording/batch fixtures: 10
- public content/layout expectation: 7
- other: 3

## Top Failure Symptoms
- editor title field not reachable: 69
- 6s timeout: 47
- 32s timeout: 23
- admin row not reachable: 18
- redirected to login: 18
- admin API 401 fixture/setup: 11
- other assertion/error: 9
- recording editor API not reachable: 6
- TOC/layout expectation mismatch: 4
- 33s timeout: 2
- ambiguous main h1 selector after h1 body headings: 2

## Top Failure Files
- tests/manual-qa-gap-coverage.spec.ts: 17
- tests/admin-search-pagination.spec.ts: 8
- tests/feature-recording-0418.spec.ts: 6
- tests/admin-dashboard.spec.ts: 5
- tests/admin-work-auto-thumbnail.spec.ts: 5
- tests/public-work-videos.spec.ts: 5
- tests/ui-admin-notion-autosave-info.spec.ts: 5
- tests/ui-admin-table-polish.spec.ts: 5
- tests/admin-blog-validation.spec.ts: 4
- tests/dark-mode.spec.ts: 4
- tests/ui-admin-notion-library-sheet.spec.ts: 4
- tests/ui-admin-unsaved-warning.spec.ts: 4
- tests/admin-blog-publish.spec.ts: 3
- tests/admin-input-exceptions.spec.ts: 3
- tests/admin-pages-settings.spec.ts: 3
- tests/admin-work-publish.spec.ts: 3
- tests/public-inline-editors.spec.ts: 3
- tests/renovation-0416-regression.spec.ts: 3
- tests/ui-admin-sidebar-active.spec.ts: 3
- tests/ui-admin-work-create-cta.spec.ts: 3

## Slowest Failures
| Duration ms | Project | File | Title |
| 318921 | chromium-authenticated | tests/e2e-admin-batch-management-journey.spec.ts:23 | e2e-admin-batch-management-journey.spec.ts › E2E-009 admin can run a batch AI workflow and then bulk delete the processed items |
| 191533 | chromium-authenticated | tests/live-blog-ai-regressions.spec.ts:54 | live-blog-ai-regressions.spec.ts › live AI and inline blog regressions › public blog detail save returns to the originating list page |
| 121841 | chromium-public | tests/feature-recording-0418.spec.ts:354 | feature-recording-0418.spec.ts › recording image resize drag |
| 121439 | chromium-public | tests/feature-recording-0418.spec.ts:327 | feature-recording-0418.spec.ts › recording mermaid editor preview |
| 121343 | chromium-public | tests/feature-recording-0418.spec.ts:340 | feature-recording-0418.spec.ts › recording mermaid work rendering |
| 121336 | chromium-public | tests/feature-recording-0418.spec.ts:366 | feature-recording-0418.spec.ts › recording image drag move |
| 121274 | chromium-public | tests/feature-recording-0418.spec.ts:360 | feature-recording-0418.spec.ts › recording image work resize |
| 121210 | chromium-public | tests/feature-recording-0418.spec.ts:303 | feature-recording-0418.spec.ts › recording mermaid rendering |
| 98822 | chromium-authenticated | tests/public-inline-editors-unsaved-warning.spec.ts:27 | public-inline-editors-unsaved-warning.spec.ts › public blog inline editor clears beforeunload after save |
| 96831 | chromium-authenticated | tests/public-inline-editors-unsaved-warning.spec.ts:62 | public-inline-editors-unsaved-warning.spec.ts › public work inline editor clears beforeunload after save |
| 63741 | chromium-authenticated | tests/ui-admin-semantic-colors.spec.ts:7 | ui-admin-semantic-colors.spec.ts › VA-005 success, warning, and error states keep distinct semantic color treatments |
| 33523 | chromium-authenticated | tests/admin-blog-publish.spec.ts:91 | admin-blog-publish.spec.ts › admin can toggle a published blog post back to draft and publish it again |

## Key Observations
- Full-suite execution is not clean in this production-like mode: 209 failed and 132 were skipped or did not run.
- A large authenticated failure cluster shows symptoms such as redirecting to `/login`, admin fixture API `401`, missing admin rows, and editor title fields not reachable.
- A post-run fresh admin smoke succeeded: `{"url":"http://woong-prod-full-e2e-nginx-1777130558/admin/dashboard","heading":"Dashboard","hasDashboard":true,"hasAdminWorkspaceError":false}`. That means the admin app can load after a fresh login; the full-suite ordering/shared storage state is likely invalidating or stale for many authenticated tests.
- Recording/batch suites add large timeouts: feature recording tests hit 120s timeouts, the admin batch journey hit about 300s, and live AI regression hit about 180s.
- Public read-only paths are comparatively healthier; the latest targeted latency run had 0 hard budget failures, and this full run also reported latency budget failures 0.

## Intentionally Not Changed
- No app code or test code was changed for this measurement.
- No Playwright timeout, retry, or budget settings were changed.
- The run used the repo's configured full suite rather than excluding recording/live/admin tests.

## Risks And Yellow Flags
- This full run mutated the shared development database and generated many ignored Playwright video/screenshot/trace artifacts under `test-results/playwright`.
- Because `video: on` and `screenshot: on` are globally enabled, the full run is expensive and artifact-heavy.
- The full suite appears order-sensitive: authenticated tests share one bootstrap storage state, while some auth/session tests intentionally log out or invalidate sessions.
- Several tests assume specific seeded content strings; current production-like DB content differs, causing public assertion failures independent of runtime performance.

## Recommendation
- First stabilize the full-suite harness before treating all 209 failures as app regressions. Split destructive auth/session tests into a separate project or regenerate authenticated storage state per test group.
- Then rerun a reduced production-like suite in layers: public read-only, admin smoke, admin editor, video/upload, recording/live AI.
- Investigate recent high-value failures separately: public work video preview tests, Notion view tests, and admin/editor readiness after fresh auth.

## Artifacts
- `backend/reports/production-like-full-e2e-2026-04-26/artifacts/full-e2e-report.json`
- `backend/reports/production-like-full-e2e-2026-04-26/artifacts/full-e2e-run.log`
- `backend/reports/production-like-full-e2e-2026-04-26/artifacts/full-e2e-summary.json`
- `backend/reports/production-like-full-e2e-2026-04-26/artifacts/full-e2e-failures.md`
- `backend/reports/production-like-full-e2e-2026-04-26/artifacts/full-e2e-failures.csv`
- `backend/reports/production-like-full-e2e-2026-04-26/artifacts/e2e-latency-summary.md`
