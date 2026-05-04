# E2E Latency Summary

Generated: 2026-05-04T16:20:18.657Z

- Tests with latency artifacts: 114
- Budget failures: 0
- Warnings: 11

## Slowest Tests

| Duration ms | Status | Project | Spec | Title |
| --- | --- | --- | --- | --- |
| 12364.77 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts | switching documents in notion view keeps the editor visible while changing the active document |
| 10846.99 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts | selected notion document persists after reload via url state |
| 10732.74 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-publish.spec.ts | admin can toggle a published work back to draft and publish it again |
| 9898.1 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts | blog notion view supports list selection and content autosave |
| 7848.22 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-042 autosave revalidation is throttled while explicit metadata save revalidates immediately |
| 6699.87 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-mixed-flow.spec.ts | admin can create a mixed work with two youtube links, two mp4 uploads, and rich body copy |
| 5948.18 | failed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-keyboard-accessibility.spec.ts | mobile admin navigation exposes a labeled nav and supports sequential keyboard focus |
| 5803.34 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts | admin can edit an existing blog post with mixed special input |
| 5787.8 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-042 autosave status waits for the autosave interval, then shows Saving and Saved after notion content changes |
| 5550.25 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-042 autosave status shows Error when notion autosave fails |
| 5240.21 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-edit-flow.spec.ts | admin can add videos inline while editing an existing work |
| 5043.87 | passed | chromium-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-blog-inline-redirects.spec.ts | public study detail inline edit returns to the originating blog page |
| 4667.91 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-inline-editors-unsaved-warning.spec.ts | public blog inline editor clears beforeunload after save |
| 4455.83 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-publish.spec.ts | admin can keep a work as draft and publish it later |
| 4381.94 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-videos.spec.ts | PF-043 public work detail keeps public video render order aligned with saved sort order |
| 4235.23 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts | AF-046 Ctrl+S saves notion content and metadata immediately |
| 3915.6 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/dark-mode.spec.ts | DM-25: global dark palette uses soft readable surfaces across public and admin UI |
| 3524.4 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-drag-order.spec.ts | AF-062 saved work videos can be reordered by drag and drop and the public order updates |
| 3253.21 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/dark-mode.spec.ts | DM-18b: public Mermaid diagrams use GitHub-readable surfaces in light and dark mode |
| 3233.01 | passed | chromium-authenticated | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-edit-flow.spec.ts | admin can reorder saved videos and delete a removed video while editing an existing work |

## Slowest API Responses

| Duration ms | Status | Method | URL | Spec |
| --- | --- | --- | --- | --- |
| 944.12 | 200 | PUT | http://127.0.0.1:3000/api/admin/blogs/c6851f50-a6c3-40c5-9849-9279a2593ea3 | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 913.9 | 500 | PUT | http://127.0.0.1:3000/api/admin/blogs/40a2b603-06aa-4dbd-98fc-a0404b4fa13c | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 430.09 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/work-inline-create-flow.spec.ts |
| 338.73 | 200 | GET | http://127.0.0.1:3000/api/auth/session | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 336.46 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts |
| 280 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 279.25 | 200 | PUT | http://127.0.0.1:3000/api/admin/blogs/1f438924-120f-44a7-b325-b54aeeddb85b | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 254.22 |  | POST | https://www.youtube-nocookie.com/api/stats/qoe?cpn=UjtGsJcseiUXQQj7&el=embedded&ns=yt&fexp=v1%2C24004644%2C494889%2C26510702%2C53408%2C34656%2C106030%2C18644%2C117689%2C9252%2C3479%2C13030%2C23206%2C69796%2C30906%2C15105%2C25059%2C4174%2C30446%2C13518%2C5188%2C21437%2C4571%2C5238%2C5800%2C1977%2C1191%2C14236%2C661%2C6620%2C2784%2C23826%2C9500%2C1840%2C22474%2C7200%2C4571%2C11470%2C13160%2C1465%2C20055%2C9390%2C1595%2C5784%2C3921%2C14341%2C3305%2C895%2C457%2C954%2C2754%2C9842%2C2498%2C9787%2C2196%2C1625%2C8030%2C3571%2C6114%2C906%2C3426%2C1304%2C8445%2C4170%2C7181%2C8954%2C2548%2C555%2C937%2C1134%2C1572%2C1286%2C3222%2C2429%2C2412%2C1540%2C490%2C4954%2C2315%2C3342%2C3257&cl=907881858&seq=1&event=streamingstats&docid=dQw4w9WgXcQ&cbr=Chrome&cbrver=145.0.7632.6&c=WEB_EMBEDDED_PLAYER&cver=2.20260501.01.00&cplayer=UNIPLAYER&cos=Windows&cosver=10.0&cplatform=DESKTOP&vps=0.000:N,0.000:ER&cmt=0.000:0.000,0.000:0.000&error=0.000:auth::0.000:0;a6s.0;r.Video_unavailable&vis=0.000:0&bh=0.000:0.000&qclc=ChBVanRHc0pjc2VpVVhRUWo3EAE | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-mixed-flow.spec.ts |
| 251.88 | 200 | GET | http://127.0.0.1:3000/api/auth/session | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 244.39 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 237.21 | 200 | GET | http://127.0.0.1:3000/api/auth/session | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 235.97 | 200 | GET | http://127.0.0.1:3000/api/auth/session | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts |
| 224.26 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-autosave-info.spec.ts |
| 213.03 |  | POST | https://www.youtube-nocookie.com/api/stats/qoe?cpn=o8BKLvkfefP36AAD&el=embedded&ns=yt&fexp=v1%2C23853953%2C150691%2C27005591%2C53408%2C34656%2C47714%2C58316%2C18644%2C117689%2C9252%2C3479%2C13030%2C23206%2C100702%2C15105%2C25059%2C4174%2C30446%2C13518%2C5188%2C21437%2C4571%2C11038%2C1978%2C15425%2C662%2C7518%2C1886%2C23826%2C9500%2C1840%2C22474%2C11771%2C11470%2C13160%2C1460%2C20060%2C10985%2C5783%2C3922%2C14341%2C3305%2C894%2C459%2C953%2C2754%2C12341%2C434%2C8148%2C1204%2C2196%2C1625%2C8030%2C3571%2C7020%2C3426%2C1304%2C8445%2C4170%2C7181%2C8954%2C2550%2C1490%2C1726%2C979%2C862%2C424%2C3222%2C2430%2C2076%2C1498%2C378%2C7759%2C3342%2C3255&cl=907881858&seq=1&event=streamingstats&docid=dQw4w9WgXcQ&cbr=Chrome&cbrver=145.0.7632.6&c=WEB_EMBEDDED_PLAYER&cver=2.20260501.01.00&cplayer=UNIPLAYER&cos=Windows&cosver=10.0&cplatform=DESKTOP&vps=0.000:N,0.000:ER&cmt=0.000:0.000,0.000:0.000&error=0.000:auth::0.000:0;a6s.0;r.Video_unavailable&vis=0.000:0&bh=0.000:0.000&qclc=ChBvOEJLTHZrZmVmUDM2QUFEEAE | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-videos.spec.ts |
| 205.43 | 204 | POST | https://www.youtube-nocookie.com/api/stats/qoe?cpn=UjtGsJcseiUXQQj7&el=embedded&ns=yt&fexp=v1%2C24004644%2C494889%2C26510702%2C53408%2C34656%2C106030%2C18644%2C117689%2C9252%2C3479%2C13030%2C23206%2C69796%2C30906%2C15105%2C25059%2C4174%2C30446%2C13518%2C5188%2C21437%2C4571%2C5238%2C5800%2C1977%2C1191%2C14236%2C661%2C6620%2C2784%2C23826%2C9500%2C1840%2C22474%2C7200%2C4571%2C11470%2C13160%2C1465%2C20055%2C9390%2C1595%2C5784%2C3921%2C14341%2C3305%2C895%2C457%2C954%2C2754%2C9842%2C2498%2C9787%2C2196%2C1625%2C8030%2C3571%2C6114%2C906%2C3426%2C1304%2C8445%2C4170%2C7181%2C8954%2C2548%2C555%2C937%2C1134%2C1572%2C1286%2C3222%2C2429%2C2412%2C1540%2C490%2C4954%2C2315%2C3342%2C3257&cl=907881858&seq=1&event=streamingstats&docid=dQw4w9WgXcQ&cbr=Chrome&cbrver=145.0.7632.6&c=WEB_EMBEDDED_PLAYER&cver=2.20260501.01.00&cplayer=UNIPLAYER&cos=Windows&cosver=10.0&cplatform=DESKTOP&vps=0.000:N,0.000:ER&cmt=0.000:0.000,0.000:0.000&error=0.000:auth::0.000:0;a6s.0;r.Video_unavailable&vis=0.000:0&bh=0.000:0.000&qclc=ChBVanRHc0pjc2VpVVhRUWo3EAE | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-mixed-flow.spec.ts |
| 203.42 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/work-inline-create-flow.spec.ts |
| 179.18 | 200 | POST | http://127.0.0.1:3000/api/admin/works/2157b37b-8a5b-4b1f-9a58-b3cc50140028/videos/hls-job | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/work-inline-create-flow.spec.ts |
| 172.91 | 200 | POST | http://127.0.0.1:3000/api/admin/works/2f8d7296-5de1-45ee-8514-7bbd92eaa703/videos/hls-job | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-video-s3-compatible.spec.ts |
| 172.77 | 200 | POST | http://127.0.0.1:3000/revalidate-public | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-publish.spec.ts |
| 165.18 | 200 | POST | http://127.0.0.1:3000/api/admin/works/e3c5bd6b-8736-4483-88c4-9367748c7980/videos/hls-job | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/public-work-videos.spec.ts |

## Slowest Interactions

| Duration ms | Name | Source | Target | Spec |
| --- | --- | --- | --- | --- |
| 1808 | pointerdown | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 1808 | mousedown | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 1808 | pointerup | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 1808 | mouseup | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 1808 | click | performance-observer | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 1747.1 | click | raf | [data-testid="notion-library-trigger"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerout | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerover | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerenter | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerenter | performance-observer | [data-testid="notion-blog-list-item"] | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerenter | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | mouseout | performance-observer | div | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | mouseover | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerdown | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | mousedown | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | pointerup | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | mouseup | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |
| 560 | click | performance-observer | p | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/ui-admin-notion-client-switch.spec.ts |

## Slowest Measured Steps

| Duration ms | Status | Step | Spec |
| --- | --- | --- | --- |
| 4148.98 | passed | Admin blog update to public detail refresh | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-edit.spec.ts |
| 1134.09 | passed | Published work public detail render after create | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-publish.spec.ts |
| 567.73 | passed | Admin work create mutation and revalidation | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-work-publish.spec.ts |
| 375.93 | passed | AI Fix dialog open to provider dropdown ready | /mnt/d/woong-blog/woong-blog-backend-architecture-renovate-0419/woong-blog-aspcore-nextjs/tests/admin-blog-ai-dialog.spec.ts |

## Budget Failures

_None._

