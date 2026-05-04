# E2E Latency Summary

Generated: 2026-05-04T16:15:13.044Z

- Tests with latency artifacts: 384
- Budget failures: 1
- Warnings: 39

## Slowest Tests

| Duration ms | Status | Project | Spec | Title |
| --- | --- | --- | --- | --- |
| 13460.27 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts | switching documents in notion view keeps the editor visible while changing the active document |
| 11705.48 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts | selected notion document persists after reload via url state |
| 11094.46 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts | blog notion view supports list selection and content autosave |
| 9611.6 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-semantic-colors.spec.ts | VA-005 success, warning, and error states keep distinct semantic color treatments |
| 9496.19 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-publish.spec.ts | admin can toggle a published blog post back to draft and publish it again |
| 9434.79 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-042 autosave revalidation is throttled while explicit metadata save revalidates immediately |
| 8285.91 | passed | chromium-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-dark-mode-journey.spec.ts | E2E-007 dark mode stays coherent across the main public journey |
| 7399.55 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts | clearing search restores the full notion document list |
| 7205.05 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-mermaid-public-independence.spec.ts | public blog and work pages stay stable when mermaid content exists |
| 7109.22 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-publish.spec.ts | admin can keep a blog post as draft and publish it later |
| 7051.24 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-videos.spec.ts | PF-043 public work detail keeps public video render order aligned with saved sort order |
| 6943.47 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts | admin can edit an existing blog post with mixed special input |
| 6817.68 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-sheet.spec.ts | selecting a document closes the sheet and keeps editor visible |
| 6811.38 | passed | chromium-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/home.spec.ts | primary navbar routes to every public destination |
| 6620.89 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-042 autosave status waits for the autosave interval, then shows Saving and Saved after notion content changes |
| 6529.59 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts | library search filters visible notion documents |
| 6419.54 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-visual-state.spec.ts | VA-242 notion save-state chip changes visual treatment across saved and error states |
| 6124.99 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-042 autosave status shows Error when notion autosave fails |
| 6034.18 | failed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-keyboard-accessibility.spec.ts | mobile admin navigation exposes a labeled nav and supports sequential keyboard focus |
| 5857.71 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-publish.spec.ts | admin can create and publish a blog post that appears on public blog page |

## Slowest API Responses

| Duration ms | Status | Method | URL | Spec |
| --- | --- | --- | --- | --- |
| 947.83 | 200 | PUT | http://127.0.0.1:3000/api/admin/blogs/b3b2e981-9cd0-431f-b862-42a854752008 | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 922.51 | 500 | PUT | http://127.0.0.1:3000/api/admin/blogs/70bdb2aa-ea2d-4ca1-a9d2-7a419fb6eecf | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 873.65 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-improvement-blog-toc.spec.ts |
| 628.69 |  | PUT | http://127.0.0.1:3000/api/admin/site-settings | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 576.42 | 200 | PUT | http://127.0.0.1:3000/api/admin/site-settings | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 527.01 | 200 | POST | http://127.0.0.1:3000/api/admin/blogs | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-delete-dialog.spec.ts |
| 478.94 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts |
| 416.97 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-blog-toc-active.spec.ts |
| 391.58 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts |
| 352.78 | 200 | GET | http://127.0.0.1:3000/api/public/works?page=1&pageSize=12&__loadTestRun=moreh0ma-qxnyv2&__loadTestUser=2&__loadTestRequest=1&__loadTestIteration=1 | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-load-test-dashboard.spec.ts |
| 352.72 | 200 | GET | http://127.0.0.1:3000/api/public/blogs?page=1&pageSize=12&__loadTestRun=moreh0ma-qxnyv2&__loadTestUser=2&__loadTestRequest=1&__loadTestIteration=1 | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-load-test-dashboard.spec.ts |
| 352.71 | 200 | GET | http://127.0.0.1:3000/api/public/works/custom-work-target?__loadTestRun=moreh0ma-qxnyv2&__loadTestUser=2&__loadTestRequest=1&__loadTestIteration=1 | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-load-test-dashboard.spec.ts |
| 348.33 | 500 | PUT | http://127.0.0.1:3000/api/admin/blogs/25b3d59a-330b-4d75-a221-80774a4cdfac | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-semantic-colors.spec.ts |
| 341.38 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-toc.spec.ts |
| 334.69 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-improvement-scroll-margin.spec.ts |
| 325.83 | 200 | GET | http://127.0.0.1:3000/api/auth/session | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 322.88 | 200 | POST | http://127.0.0.1:3000/api/admin/works/e64709fe-8698-4c23-a259-ffefc082b1c6/videos/hls-job | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-videos.spec.ts |
| 320.36 | 200 | GET | http://127.0.0.1:3000/admin/dashboard | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-improvement-blog-toc.spec.ts |
| 316.38 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-table-polish.spec.ts |
| 314.52 |  | POST | https://www.youtube-nocookie.com/api/stats/qoe?cpn=r_OeUVsA0W7mmi7A&el=embedded&ns=yt&fexp=v1%2C24004644%2C27005591%2C53408%2C34656%2C106030%2C18644%2C117689%2C9252%2C3479%2C13030%2C6258%2C16948%2C35401%2C33147%2C1248%2C30906%2C15105%2C25059%2C4174%2C30446%2C13518%2C5188%2C21437%2C4571%2C11038%2C1978%2C16087%2C9404%2C23826%2C9500%2C1840%2C22474%2C1645%2C10126%2C11471%2C10930%2C2229%2C21520%2C10455%2C529%2C9706%2C14341%2C3305%2C894%2C460%2C952%2C2754%2C9842%2C2933%2C9350%2C2197%2C1626%2C8030%2C3571%2C7020%2C3426%2C1304%2C8445%2C4170%2C7181%2C8954%2C2549%2C1491%2C2704%2C1288%2C3222%2C2257%2C172%2C2411%2C1172%2C369%2C5444%2C2314%2C3342%2C3258&cl=907881858&seq=1&event=streamingstats&docid=dQw4w9WgXcQ&cbr=Chrome&cbrver=145.0.7632.6&c=WEB_EMBEDDED_PLAYER&cver=2.20260501.01.00&cplayer=UNIPLAYER&cos=Windows&cosver=10.0&cplatform=DESKTOP&vps=0.000:N,0.000:ER&cmt=0.000:0.000,0.000:0.000&error=0.000:auth::0.000:0;a6s.0;r.Video_unavailable&vis=0.000:0&bh=0.000:0.000&qclc=ChByX09lVVZzQTBXN21taTdBEAE | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-videos.spec.ts |

## Slowest Interactions

| Duration ms | Name | Source | Target | Spec |
| --- | --- | --- | --- | --- |
| 1880 | pointerdown | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1880 | mousedown | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1880 | pointerup | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1880 | mouseup | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1880 | click | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1828.7 | click | raf | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerover | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer |  | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | html | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | body | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | main | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerenter | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | mouseover | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerdown | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | mousedown | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |
| 1744 | pointerup | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-library-search.spec.ts |

## Slowest Measured Steps

| Duration ms | Status | Step | Spec |
| --- | --- | --- | --- |
| 3766.27 | passed | Admin blog update to public detail refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts |
| 3396.59 | passed | Admin blog create to public detail refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-publish.spec.ts |
| 1714.3 | passed | Public nav click to Works | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 885.38 | passed | Admin site settings save response-time path | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 852.08 | passed | Admin work bulk delete confirmation to backend response | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-bulk-delete.spec.ts |
| 727.75 | passed | Admin blog bulk delete confirmation to backend response | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-bulk-delete.spec.ts |
| 702.02 | passed | Admin introduction page save to public refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-pages-settings.spec.ts |
| 686.65 | passed | Public Work card opens detail | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 578.82 | passed | Works list direct load to primary content visible | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 570.46 | passed | Admin resume upload to public resume refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-resume-upload.spec.ts |
| 533.72 | passed | Works unified search submit | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-works-search.spec.ts |
| 525.75 | passed | Admin contact page save to public refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-pages-settings.spec.ts |
| 482.54 | passed | Public Study card opens detail | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 459.74 | passed | Admin resume delete to public resume refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-resume-upload.spec.ts |
| 433.68 | passed | Study list direct load to primary content visible | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 306.52 | passed | Admin site settings save to public home refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-pages-settings.spec.ts |
| 233.7 | passed | Study unified search submit response-time path | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 191.85 | passed | Works desktop pagination next navigation | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-works-pagination.spec.ts |
| 177.88 | passed | AI Fix provider dropdown response-time path | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |
| 176.03 | passed | Study unified search submit | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-works-search.spec.ts |

## Budget Failures

| Severity | Duration ms | Hard ms | Kind | Name | Spec |
| --- | --- | --- | --- | --- | --- |
| hard | 1714.3 | 1500 | step | Public nav click to Works | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/e2e-response-time.spec.ts |

