# E2E Latency Summary

Generated: 2026-04-25T17:37:43.710Z

- Tests with latency artifacts: 466
- Budget failures: 0
- Warnings: 113

## Slowest Tests

| Duration ms | Status | Project | Spec | Title |
| --- | --- | --- | --- | --- |
| 300139.45 | timedOut | chromium-authenticated | /work/tests/e2e-admin-batch-management-journey.spec.ts | E2E-009 admin can run a batch AI workflow and then bulk delete the processed items |
| 180175.74 | timedOut | chromium-authenticated | /work/tests/live-blog-ai-regressions.spec.ts | public blog detail save returns to the originating list page |
| 120241.32 | timedOut | chromium-public | /work/tests/feature-recording-0418.spec.ts | recording mermaid editor preview |
| 120225.58 | timedOut | chromium-public | /work/tests/feature-recording-0418.spec.ts | recording mermaid work rendering |
| 120219.73 | timedOut | chromium-public | /work/tests/feature-recording-0418.spec.ts | recording image resize drag |
| 120211.49 | timedOut | chromium-public | /work/tests/feature-recording-0418.spec.ts | recording image work resize |
| 120205.12 | timedOut | chromium-public | /work/tests/feature-recording-0418.spec.ts | recording image drag move |
| 120110.01 | timedOut | chromium-public | /work/tests/feature-recording-0418.spec.ts | recording mermaid rendering |
| 90366.94 | timedOut | chromium-authenticated | /work/tests/public-inline-editors-unsaved-warning.spec.ts | public blog inline editor clears beforeunload after save |
| 90239.37 | timedOut | chromium-authenticated | /work/tests/public-inline-editors-unsaved-warning.spec.ts | public work inline editor clears beforeunload after save |
| 60188.8 | timedOut | chromium-authenticated | /work/tests/ui-admin-semantic-colors.spec.ts | VA-005 success, warning, and error states keep distinct semantic color treatments |
| 57334.22 | passed | chromium-public | /work/tests/responsive-width-sweep.spec.ts | capture homepage across a full width sweep and record layout metrics |
| 30326.1 | timedOut | chromium-authenticated | /work/tests/admin-blog-publish.spec.ts | admin can keep a blog post as draft and publish it later |
| 30251.73 | timedOut | chromium-authenticated | /work/tests/admin-pages-settings.spec.ts | admin can update the introduction page from admin pages |
| 30227.98 | timedOut | chromium-authenticated | /work/tests/work-green-video-thumbnail.spec.ts | uploaded green-only mp4 generates and persists an automatic thumbnail |
| 30200.92 | timedOut | chromium-authenticated | /work/tests/admin-work-publish.spec.ts | admin can toggle a published work back to draft and publish it again |
| 30197.76 | timedOut | chromium-authenticated | /work/tests/ui-admin-work-create-cta.spec.ts | work create CTA stays consolidated when multiple videos are staged |
| 30180.51 | timedOut | chromium-authenticated | /work/tests/manual-qa-gap-coverage.spec.ts | F-5 and F-6 inline create flows can create works and blog posts from public pages |
| 30179.45 | timedOut | chromium-authenticated | /work/tests/public-inline-editors.spec.ts | resume page exposes inline PDF upload shell for admins |
| 30179.28 | timedOut | chromium-authenticated | /work/tests/manual-qa-gap-coverage.spec.ts | F-3 and F-4 inline page editors can save introduction and contact content in place |

## Slowest API Responses

| Duration ms | Status | Method | URL | Spec |
| --- | --- | --- | --- | --- |
| 2434.27 | 200 | POST | http://woong-prod-full-e2e-nginx-1777130558/api/admin/blogs | /work/tests/feature-recording-0418.spec.ts |
| 1297.71 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/csrf | /work/tests/feature-recording-0418.spec.ts |
| 737.97 | 200 | POST | http://woong-prod-full-e2e-nginx-1777130558/api/admin/works | /work/tests/feature-recording-0418.spec.ts |
| 571.28 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/public-blog-inline-redirects.spec.ts |
| 273.81 | 302 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/test-login?email=admin%40example.com&returnUrl=%2Fadmin%2Fdashboard | /work/tests/test-server-runtime.spec.ts |
| 252.85 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/public/blogs?page=2&pageSize=10 | /work/tests/public-blog-pagination.spec.ts |
| 131.04 |  | POST | http://woong-prod-full-e2e-nginx-1777130558/api/auth/logout?returnUrl=%2F | /work/tests/manual-qa-auth-gap.spec.ts |
| 126.84 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/e2e-dark-mode-journey.spec.ts |
| 124.21 |  | POST | http://woong-prod-full-e2e-nginx-1777130558/api/auth/logout?returnUrl=%2Flogin | /work/tests/admin-auth-session-expiry.spec.ts |
| 116.2 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/public-work-toc.spec.ts |
| 109.69 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/ui-improvement-related-page-retention.spec.ts |
| 76 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/csrf | /work/tests/admin-auth-session-expiry.spec.ts |
| 74.94 | 200 | POST | http://woong-prod-full-e2e-nginx-1777130558/api/admin/blogs | /work/tests/feature-recording-0418.spec.ts |
| 73.44 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/ui-improvement-badge-color.spec.ts |
| 64.11 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/ui-improvement-badge-color.spec.ts |
| 54.04 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/public/blogs?page=2&pageSize=10 | /work/tests/public-blog-pagination.spec.ts |
| 53.26 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/public/works?page=1&pageSize=100 | /work/tests/video-preview-recording-0424.spec.ts |
| 49.52 | 200 | GET | http://woong-prod-full-e2e-nginx-1777130558/api/auth/session | /work/tests/public-layout-stability.spec.ts |
| 48.48 | 200 | PUT | http://woong-prod-full-e2e-nginx-1777130558/api/admin/site-settings | /work/tests/auth-security-browser.spec.ts |
| 47.36 | 200 | PUT | http://woong-prod-full-e2e-nginx-1777130558/api/admin/pages | /work/tests/public-page-inline-save.spec.ts |

## Slowest Interactions

| Duration ms | Name | Source | Target | Spec |
| --- | --- | --- | --- | --- |
| 144 | keydown | performance-observer | a | /work/tests/ui-improvement-focus-visible.spec.ts |
| 128 | pointerover | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer |  | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | html | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | body | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | div | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | header | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | div | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | div | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | [data-testid="theme-toggle"] | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerenter | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | mouseover | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerdown | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | mousedown | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | pointerup | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | mouseup | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 128 | click | performance-observer | span | /work/tests/renovation-0416-regression.spec.ts |
| 120 | keyup | performance-observer | a | /work/tests/ui-improvement-focus-visible.spec.ts |
| 112 | pointerout | performance-observer | a | /work/tests/home.spec.ts |
| 112 | pointerleave | performance-observer | a | /work/tests/home.spec.ts |

## Slowest Measured Steps

| Duration ms | Status | Step | Spec |
| --- | --- | --- | --- |
| 707.72 | passed | Public nav click to Works | /work/tests/e2e-response-time.spec.ts |
| 621.6 | passed | Works unified search submit | /work/tests/public-works-search.spec.ts |
| 595.05 | passed | Admin site settings save response-time path | /work/tests/e2e-response-time.spec.ts |
| 586.99 | passed | Study unified search submit | /work/tests/public-works-search.spec.ts |
| 570.46 | passed | Study unified search submit response-time path | /work/tests/e2e-response-time.spec.ts |
| 536.95 | passed | Public nav click to Contact | /work/tests/e2e-response-time.spec.ts |
| 519.71 | passed | Public Study card opens detail | /work/tests/e2e-response-time.spec.ts |
| 496.86 | passed | Public Work card opens detail | /work/tests/e2e-response-time.spec.ts |
| 415.39 | passed | Study mobile auto append next page | /work/tests/public-blog-pagination.spec.ts |
| 358.58 | passed | Works desktop pagination next navigation | /work/tests/public-works-pagination.spec.ts |
| 302.83 | passed | Study mobile auto append before detail navigation | /work/tests/public-blog-pagination.spec.ts |
| 297.11 | passed | Works list direct load to primary content visible | /work/tests/e2e-response-time.spec.ts |
| 259.44 | passed | Study list direct load to primary content visible | /work/tests/e2e-response-time.spec.ts |
| 258.72 | passed | Public nav click to Introduction | /work/tests/e2e-response-time.spec.ts |
| 175.69 | passed | Public nav click to Study | /work/tests/e2e-response-time.spec.ts |
| 170.71 | passed | Works mobile auto append next page | /work/tests/public-works-pagination.spec.ts |
| 169.84 | passed | AI Fix provider dropdown response-time path | /work/tests/e2e-response-time.spec.ts |
| 131.01 | passed | Study mobile auto-append | /work/tests/e2e-response-time.spec.ts |
| 85.13 | passed | Works tablet load more appends next page | /work/tests/public-works-pagination.spec.ts |
| 71.91 | passed | Study tablet load more appends next page | /work/tests/public-blog-pagination.spec.ts |

## Budget Failures

_None._

