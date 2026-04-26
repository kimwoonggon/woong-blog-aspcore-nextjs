# Full E2E Failure List (production-like-full-e2e-2026-04-26)

- Total tests: 589
- Passed: 248
- Failed: 209
- Skipped or did not run: 132 (terminal: 14 skipped, 118 did not run)
- Duration: 2h 15m 5s

## Failures By Category
- admin/editor/authenticated UI: 144
- works video/admin media: 24
- admin notion view: 21
- recording/batch fixtures: 10
- public content/layout expectation: 7
- other: 3

## Failures By Symptom
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
- tests/ui-quality-visual-metrics.spec.ts: 3
- tests/work-inline-create-flow.spec.ts: 3
- tests/work-inline-redirects.spec.ts: 3
- tests/admin-ai-batch-cancel.spec.ts: 2
- tests/admin-blog-edit.spec.ts: 2
- tests/admin-dashboard-error-state.spec.ts: 2
- tests/admin-resume-upload.spec.ts: 2
- tests/admin-work-validation.spec.ts: 2
- tests/admin-work-video-edit-flow.spec.ts: 2
- tests/public-inline-editors-unsaved-warning.spec.ts: 2

## All Failures
1. [chromium-public] tests/e2e-visitor-content-exploration.spec.ts:4 e2e-visitor-content-exploration.spec.ts › E2E-003 visitor can move from home to works, through related content, and into blog details
   - result: failed, duration: 2852ms
   - category: other
   - symptom: ambiguous main h1 selector after h1 body headings
   - error: Error: expect(locator).toHaveText(expected) failed Locator: locator('main h1') Expected: "affadfad" Error: strict mode violation: locator('main h1') resolved to 17 elements: 1) <h1 class="mb-4 text-3xl font-heading font-bold leading-tight text-foreground text-balance md:text-4xl">affadfad</h1> aka getByTestId('work-detail-body').getByRole('heading', { name: 'affadfad' }) 2) <h1 id="1">1</h1> aka getByRole('heading', { name: '1', exact: true }) 3) <h1 id="2">2</h1> aka getByRole('heading', { name
2. [chromium-public] tests/feature-recording-0418.spec.ts:303 feature-recording-0418.spec.ts › recording mermaid rendering
   - result: timedOut, duration: 121210ms
   - category: recording/batch fixtures
   - symptom: recording editor API not reachable
   - error: Test timeout of 120000ms exceeded. Error: page.waitForFunction: Target page, context or browser has been closed 129 | 130 | async function waitForEditorApi(page: Page) { > 131 | await page.waitForFunction(() => Boolean((window as typeof window & { __WOONG_TIPTAP_EDITOR__?: unknown }).__WOONG_TIPTAP_EDITOR__)) | ^ 132 | } 133 | 134 | async function setEditorHtml(page: Page, html: string) { at waitForEditorApi (/work/tests/feature-recording-0418.spec.ts:131:14) at setEditorHtml (/work/tests/featur
3. [chromium-public] tests/feature-recording-0418.spec.ts:327 feature-recording-0418.spec.ts › recording mermaid editor preview
   - result: timedOut, duration: 121439ms
   - category: recording/batch fixtures
   - symptom: recording editor API not reachable
   - error: Test timeout of 120000ms exceeded. Error: page.waitForFunction: Target page, context or browser has been closed 129 | 130 | async function waitForEditorApi(page: Page) { > 131 | await page.waitForFunction(() => Boolean((window as typeof window & { __WOONG_TIPTAP_EDITOR__?: unknown }).__WOONG_TIPTAP_EDITOR__)) | ^ 132 | } 133 | 134 | async function setEditorHtml(page: Page, html: string) { at waitForEditorApi (/work/tests/feature-recording-0418.spec.ts:131:14) at setEditorHtml (/work/tests/featur
4. [chromium-public] tests/feature-recording-0418.spec.ts:340 feature-recording-0418.spec.ts › recording mermaid work rendering
   - result: timedOut, duration: 121343ms
   - category: recording/batch fixtures
   - symptom: recording editor API not reachable
   - error: Test timeout of 120000ms exceeded. Error: page.waitForFunction: Target page, context or browser has been closed 129 | 130 | async function waitForEditorApi(page: Page) { > 131 | await page.waitForFunction(() => Boolean((window as typeof window & { __WOONG_TIPTAP_EDITOR__?: unknown }).__WOONG_TIPTAP_EDITOR__)) | ^ 132 | } 133 | 134 | async function setEditorHtml(page: Page, html: string) { at waitForEditorApi (/work/tests/feature-recording-0418.spec.ts:131:14) at setEditorHtml (/work/tests/featur
5. [chromium-public] tests/feature-recording-0418.spec.ts:354 feature-recording-0418.spec.ts › recording image resize drag
   - result: timedOut, duration: 121841ms
   - category: recording/batch fixtures
   - symptom: recording editor API not reachable
   - error: Test timeout of 120000ms exceeded. Error: page.waitForFunction: Target page, context or browser has been closed 129 | 130 | async function waitForEditorApi(page: Page) { > 131 | await page.waitForFunction(() => Boolean((window as typeof window & { __WOONG_TIPTAP_EDITOR__?: unknown }).__WOONG_TIPTAP_EDITOR__)) | ^ 132 | } 133 | 134 | async function setEditorHtml(page: Page, html: string) { at waitForEditorApi (/work/tests/feature-recording-0418.spec.ts:131:14) at setEditorHtml (/work/tests/featur
6. [chromium-public] tests/feature-recording-0418.spec.ts:360 feature-recording-0418.spec.ts › recording image work resize
   - result: timedOut, duration: 121274ms
   - category: recording/batch fixtures
   - symptom: recording editor API not reachable
   - error: Test timeout of 120000ms exceeded. Error: page.waitForFunction: Target page, context or browser has been closed 129 | 130 | async function waitForEditorApi(page: Page) { > 131 | await page.waitForFunction(() => Boolean((window as typeof window & { __WOONG_TIPTAP_EDITOR__?: unknown }).__WOONG_TIPTAP_EDITOR__)) | ^ 132 | } 133 | 134 | async function setEditorHtml(page: Page, html: string) { at waitForEditorApi (/work/tests/feature-recording-0418.spec.ts:131:14) at setEditorHtml (/work/tests/featur
7. [chromium-public] tests/feature-recording-0418.spec.ts:366 feature-recording-0418.spec.ts › recording image drag move
   - result: timedOut, duration: 121336ms
   - category: recording/batch fixtures
   - symptom: recording editor API not reachable
   - error: Test timeout of 120000ms exceeded. Error: page.waitForFunction: Target page, context or browser has been closed 129 | 130 | async function waitForEditorApi(page: Page) { > 131 | await page.waitForFunction(() => Boolean((window as typeof window & { __WOONG_TIPTAP_EDITOR__?: unknown }).__WOONG_TIPTAP_EDITOR__)) | ^ 132 | } 133 | 134 | async function setEditorHtml(page: Page, html: string) { at waitForEditorApi (/work/tests/feature-recording-0418.spec.ts:131:14) at setEditorHtml (/work/tests/featur
8. [chromium-public] tests/mermaid-batch-prompt-0419.spec.ts:126 mermaid-batch-prompt-0419.spec.ts › save batch ai prompt before reading mermaid posts
   - result: failed, duration: 0ms
   - category: recording/batch fixtures
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 96 | }, 97 | }) > 98 | expect(response.ok()).toBeTruthy() | ^ 99 | const payload = await response.json() as { id: string; slug: string } 100 | return { ...payload, title } 101 | } at createBlog (/work/tests/mermaid-batch-prompt-0419.spec.ts:98:25) at /work/tests/mermaid-batch-prompt-0419.spec.ts:107:20
9. [chromium-public] tests/mermaid-worstcase-0419.spec.ts:112 mermaid-worstcase-0419.spec.ts › admin mermaid blog #01
   - result: failed, duration: 0ms
   - category: recording/batch fixtures
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 81 | }, 82 | }) > 83 | expect(response.ok()).toBeTruthy() | ^ 84 | const payload = await response.json() as { id: string; slug: string } 85 | return { ...payload, title } 86 | } at createBlog (/work/tests/mermaid-worstcase-0419.spec.ts:83:25) at /work/tests/mermaid-worstcase-0419.spec.ts:92:20
10. [chromium-public] tests/plain-baseline-0419.spec.ts:96 plain-baseline-0419.spec.ts › admin plain blog #01
   - result: failed, duration: 0ms
   - category: recording/batch fixtures
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 65 | }, 66 | }) > 67 | expect(response.ok()).toBeTruthy() | ^ 68 | const payload = await response.json() as { id: string; slug: string } 69 | return { ...payload, title } 70 | } at createBlog (/work/tests/plain-baseline-0419.spec.ts:67:25) at /work/tests/plain-baseline-0419.spec.ts:76:20
11. [chromium-public] tests/public-api-error.spec.ts:3 public-api-error.spec.ts › CF-005 shows a user-facing fallback when a public page payload is malformed
   - result: failed, duration: 5778ms
   - category: public content/layout expectation
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByText('Public pages') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByText('Public pages') 4 | await page.goto('/introduction?__qaBroken=1') 5 | > 6 | await expect(page.getByText('Public pages')).toBeVisible() | ^ 7 | await expect(page.getByRole('heading', { name: 'This page could not be loaded.' })).toBeVisible() 8 | await expect(page.getByRole('button',
12. [chromium-public] tests/public-blog-toc-active.spec.ts:3 public-blog-toc-active.spec.ts › blog detail TOC updates its active heading as the reader moves deeper into the article
   - result: failed, duration: 1188ms
   - category: public content/layout expectation
   - symptom: TOC/layout expectation mismatch
   - error: Error: expect(received).toBeGreaterThan(expected) Expected: > 1 Received: 0 9 | 10 | const links = toc.getByRole('link') > 11 | expect(await links.count()).toBeGreaterThan(1) | ^ 12 | 13 | const firstLink = links.first() 14 | const secondLink = links.nth(1) at /work/tests/public-blog-toc-active.spec.ts:11:31
13. [chromium-public] tests/public-detail-pages.spec.ts:9 public-detail-pages.spec.ts › work detail page renders seeded detail content and stable related cards
   - result: failed, duration: 1220ms
   - category: public content/layout expectation
   - symptom: ambiguous main h1 selector after h1 body headings
   - error: Error: expect(locator).toHaveText(expected) failed Locator: locator('main h1') Expected: "affadfad" Error: strict mode violation: locator('main h1') resolved to 17 elements: 1) <h1 class="mb-4 text-3xl font-heading font-bold leading-tight text-foreground text-balance md:text-4xl">affadfad</h1> aka getByRole('heading', { name: 'affadfad' }) 2) <h1>1</h1> aka getByRole('heading', { name: '1', exact: true }) 3) <h1>2</h1> aka getByRole('heading', { name: '2', exact: true }) 4) <h1>3</h1> aka getByR
14. [chromium-public] tests/public-resume-empty-state.spec.ts:3 public-resume-empty-state.spec.ts › PF-072 shows the no-resume message when no resume PDF is configured
   - result: failed, duration: 5705ms
   - category: public content/layout expectation
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveCount(expected) failed Locator: getByRole('link', { name: /Download PDF/i }) Expected: 0 Received: 1 Timeout: 5000ms Call log: - Expect "toHaveCount" with timeout 5000ms - waiting for getByRole('link', { name: /Download PDF/i }) 9 × locator resolved to 1 element - unexpected value "1" 6 | 7 | await expect(page.getByRole('heading', { name: 'Resume', exact: true })).toBeVisible() > 8 | await expect(page.getByRole('link', { name: /Download PDF/i })).toHaveCount(0) | ^ 9
15. [chromium-public] tests/ui-improvement-static-public-pages.spec.ts:27 ui-improvement-static-public-pages.spec.ts › static public pages keep a clean unframed header pattern
   - result: failed, duration: 6008ms
   - category: public content/layout expectation
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveText(expected) failed Locator: locator('main header').first().locator('h1').first() Expected: "Introduction" Received: "Introductiondsffds" Timeout: 5000ms Call log: - Expect "toHaveText" with timeout 5000ms - waiting for locator('main header').first().locator('h1').first() 9 × locator resolved to <h1 class="text-3xl font-heading font-bold text-foreground md:text-4xl">Introductiondsffds</h1> - unexpected value "Introductiondsffds" 37 | const header = page.locator('ma
16. [chromium-authenticated] tests/admin-ai-batch-cancel.spec.ts:18 admin-ai-batch-cancel.spec.ts › admin can cancel a running blog AI batch job from the batch panel
   - result: timedOut, duration: 32289ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByRole('checkbox') 134 | await page.goto('/admin/blog') 135 | const rows = page.getByTestId('admin-blog-row') > 136 | await rows.nth(0).getByRole('checkbox').click() | ^ 137 | await rows.nth(1).getByRole('checkbox').click() 138 | await page.getByRole('button', { name: 'Batch AI Fix' }).click() 139 | at /work/tests/admin-ai-batch-cancel.spec.ts
17. [chromium-authenticated] tests/admin-ai-batch-cancel.spec.ts:151 admin-ai-batch-cancel.spec.ts › admin can cancel queued blog AI batch jobs in bulk from the batch panel
   - result: timedOut, duration: 32428ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByRole('checkbox') 287 | await page.goto('/admin/blog') 288 | const rows = page.getByTestId('admin-blog-row') > 289 | await rows.nth(0).getByRole('checkbox').click() | ^ 290 | await rows.nth(1).getByRole('checkbox').click() 291 | await page.getByRole('button', { name: 'Batch AI Fix' }).click() 292 | at /work/tests/admin-ai-batch-cancel.spec.ts
18. [chromium-authenticated] tests/admin-ai-batch-jobs.spec.ts:5 admin-ai-batch-jobs.spec.ts › admin can create, observe, and apply a blog AI batch job without blocking the workspace
   - result: failed, duration: 6705ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Blog Posts' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Blog Posts' }) 149 | 150 | await page.goto('/admin/blog') > 151 | await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible() | ^ 152 | 153 | const rows = page.getByTestId('admin-blog-row') 154 | const count = await rows.coun
19. [chromium-authenticated] tests/admin-blog-ai-dialog.spec.ts:6 admin-blog-ai-dialog.spec.ts › blog AI fix dialog loads runtime config, applies a fixed draft, and keeps editing local
   - result: timedOut, duration: 33107ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 39 | 40 | await page.goto('/admin/blog/new') > 41 | await page.getByLabel('Title').fill(`AI Fix Coverage ${Date.now()}`) | ^ 42 | await page.locator('form .tiptap.ProseMirror').first().click() 43 | await page.keyboard.type('rough draft before ai fix') 44 | at /work/tests/admin-blog-ai-dialog.spec.ts:41:34
20. [chromium-authenticated] tests/admin-blog-edit.spec.ts:9 admin-blog-edit.spec.ts › admin can edit an existing blog post with mixed special input
   - result: failed, duration: 636ms
   - category: admin/editor/authenticated UI
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/admin-blog-edit.spec.ts:12:16
21. [chromium-authenticated] tests/admin-blog-edit.spec.ts:51 admin-blog-edit.spec.ts › blog notion view supports list selection and content autosave
   - result: failed, duration: 548ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/admin-blog-edit.spec.ts:53:21
22. [chromium-authenticated] tests/admin-blog-image-upload.spec.ts:7 admin-blog-image-upload.spec.ts › blog editor uploads an inline image and public blog renders it
   - result: failed, duration: 5903ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 9 | 10 | await page.goto('/admin/blog/new') > 11 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 12 | 13 | await page.getByLabel('Title').fill(title) 14 | await expect(page.getByText("New posts go
23. [chromium-authenticated] tests/admin-blog-image-validation.spec.ts:7 admin-blog-image-validation.spec.ts › blog editor keeps editing state when inline image upload fails
   - result: failed, duration: 6053ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 15 | 16 | await page.goto('/admin/blog/new') > 17 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 18 | 19 | await page.getByLabel('Title').fill(`이미지 실패 블로그 ${Date.now()}`) 20 | at /work/tests/admi
24. [chromium-authenticated] tests/admin-blog-publish.spec.ts:7 admin-blog-publish.spec.ts › admin can create and publish a blog post that appears on public blog page
   - result: failed, duration: 6239ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 10 | await page.goto('/admin/blog/new') 11 | > 12 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 13 | await expect(page.getByText("New posts go live immediately. Toggle 'Published' off to save as
25. [chromium-authenticated] tests/admin-blog-publish.spec.ts:42 admin-blog-publish.spec.ts › admin can keep a blog post as draft and publish it later
   - result: timedOut, duration: 32703ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 44 | 45 | await page.goto('/admin/blog/new') > 46 | await page.getByLabel('Title').fill(title) | ^ 47 | await page.getByLabel('Tags (comma separated)').fill('playwright, draft') 48 | await page.getByRole('checkbox', { name: 'Published' }).uncheck() 49 | at /work/tests/admin-blog-publish.spec.ts:46:34
26. [chromium-authenticated] tests/admin-blog-publish.spec.ts:91 admin-blog-publish.spec.ts › admin can toggle a published blog post back to draft and publish it again
   - result: timedOut, duration: 33523ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 93 | 94 | await page.goto('/admin/blog/new') > 95 | await page.getByLabel('Title').fill(title) | ^ 96 | await page.getByLabel('Tags (comma separated)').fill('playwright, toggle') 97 | 98 | const editor = page.locator('.tiptap.ProseMirror').first() at /work/tests/admin-blog-publish.spec.ts:95:34
27. [chromium-authenticated] tests/admin-blog-validation.spec.ts:5 admin-blog-validation.spec.ts › blog editor requires a title before it enables create
   - result: failed, duration: 6111ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 5 | test('blog editor requires a title before it enables create', async ({ page }) => { 6 | await page.goto('/admin/blog/new') > 7 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 8 | 9 | const tit
28. [chromium-authenticated] tests/admin-blog-validation.spec.ts:20 admin-blog-validation.spec.ts › blog editor clamps excerpt length to preview-safe limits
   - result: failed, duration: 5993ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 20 | test('blog editor clamps excerpt length to preview-safe limits', async ({ page }) => { 21 | await page.goto('/admin/blog/new') > 22 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 23 | 24 | c
29. [chromium-authenticated] tests/admin-blog-validation.spec.ts:33 admin-blog-validation.spec.ts › blog editor accepts mixed special-character and Korean input
   - result: failed, duration: 5910ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 35 | 36 | await page.goto('/admin/blog/new') > 37 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 38 | 39 | await page.getByLabel('Title').fill(title) 40 | await page.getByLabel('Tags (comma separ
30. [chromium-authenticated] tests/admin-blog-validation.spec.ts:54 admin-blog-validation.spec.ts › blog editor shows backend validation feedback without leaving the editor
   - result: failed, duration: 5942ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/blog\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 67 | 68 | await page.goto('/admin/blog/new') > 69 | await expect(page).toHaveURL(/\/admin\/blog\/new/) | ^ 70 | 71 | await page.getByLabel('Title').fill(`Validation Failure ${Date.now()}`) 72 | await page.
31. [chromium-authenticated] tests/admin-dashboard-error-state.spec.ts:5 admin-dashboard-error-state.spec.ts › AF-013 shows the dashboard error panel when stats loading fails
   - result: failed, duration: 5801ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Dashboard', exact: true }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Dashboard', exact: true }) 6 | await page.goto('/admin/dashboard?__qaSummaryFail=1') 7 | > 8 | await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible() | ^ 9 | await expect(page.getByRole('heading',
32. [chromium-authenticated] tests/admin-dashboard-error-state.spec.ts:16 admin-dashboard-error-state.spec.ts › AF-013 shows the dashboard collections fallback when work and blog lists fail
   - result: failed, duration: 5867ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Dashboard', exact: true }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Dashboard', exact: true }) 17 | await page.goto('/admin/dashboard?__qaCollectionsFail=1') 18 | > 19 | await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible() | ^ 20 | await expect(page.getByText('T
33. [chromium-authenticated] tests/admin-dashboard.spec.ts:6 admin-dashboard.spec.ts › admin dashboard shows summary cards with numeric counts
   - result: failed, duration: 5853ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Dashboard' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Dashboard' }) 7 | await page.goto('/admin/dashboard') 8 | > 9 | await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible() | ^ 10 | await expect(page.getByText('Total Views')).toBeVisible() 11 | await expect(page.getByText('To
34. [chromium-authenticated] tests/admin-dashboard.spec.ts:26 admin-dashboard.spec.ts › admin dashboard cards open edit pages directly and expose pagination controls
   - result: failed, duration: 5923ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Works' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Works' }) 27 | await page.goto('/admin/dashboard') 28 | > 29 | await expect(page.getByRole('heading', { name: 'Works' })).toBeVisible() | ^ 30 | await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible() 31 | await expect(page.ge
35. [chromium-authenticated] tests/admin-dashboard.spec.ts:51 admin-dashboard.spec.ts › admin sidebar exposes a direct public site shortcut
   - result: failed, duration: 5915ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('link', { name: 'View Site' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('link', { name: 'View Site' }) 53 | 54 | const shortcut = page.getByRole('link', { name: 'View Site' }) > 55 | await expect(shortcut).toBeVisible() | ^ 56 | await expect(shortcut).toHaveAttribute('href', '/') 57 | await expect(shortcut).toHaveAttribute('target', '_bl
36. [chromium-authenticated] tests/admin-dashboard.spec.ts:60 admin-dashboard.spec.ts › admin dashboard quick navigation links point to the site, members, and notion workspace
   - result: failed, duration: 5892ms
   - category: admin notion view
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveAttribute(expected) failed Locator: locator('main').getByRole('link', { name: 'Open Site' }) Expected: "/" Timeout: 5000ms Error: element(s) not found Call log: - Expect "toHaveAttribute" with timeout 5000ms - waiting for locator('main').getByRole('link', { name: 'Open Site' }) 65 | const notionLink = page.locator('main').getByRole('link', { name: 'Blog Notion View' }) 66 | > 67 | await expect(openSiteLink).toHaveAttribute('href', '/') | ^ 68 | await expect(membersLi
37. [chromium-authenticated] tests/admin-dashboard.spec.ts:72 admin-dashboard.spec.ts › admin dashboard recent content sections show linked titles and summary metadata
   - result: failed, duration: 5971ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('works-card-link').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('works-card-link').first() 76 | const firstBlogCard = page.getByTestId('blog-posts-card-link').first() 77 | > 78 | await expect(firstWorkCard).toBeVisible() | ^ 79 | await expect(firstWorkCard).toHaveAttribute('href', /\/admin\/works\/.+\?returnTo=%2Fadmin%2Fdashboar
38. [chromium-authenticated] tests/admin-home-extreme-input.spec.ts:6 admin-home-extreme-input.spec.ts › home page editor persists mixed Korean, English, and punctuation input
   - result: timedOut, duration: 32200ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('heading', { name: 'Home Page - Hero Section' }).locator('xpath=ancestor::div[contains(@class, "space-y-6")][1]').getByLabel('Headline') 14 | await page.goto('/admin/pages') 15 | const homeEditor = page.getByRole('heading', { name: 'Home Page - Hero Section' }).locator('xpath=ancestor::div[contains(@class, "space-y-6")][1]') > 16 | await homeEditor.getByLabel('Headline').fil
39. [chromium-authenticated] tests/admin-home-image-upload.spec.ts:7 admin-home-image-upload.spec.ts › home page editor uploads a profile image and public home renders it
   - result: failed, duration: 5872ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/pages/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 11 | 12 | await page.goto('/admin/pages') > 13 | await expect(page).toHaveURL(/\/admin\/pages/) | ^ 14 | 15 | const homeSection = page.locator('#home-page-editor') 16 | await expect(homeSection.getByText('Home
40. [chromium-authenticated] tests/admin-home-image-validation.spec.ts:6 admin-home-image-validation.spec.ts › home page image upload shows explicit error when backend upload fails
   - result: failed, duration: 5895ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/pages/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 20 | 21 | await page.goto('/admin/pages') > 22 | await expect(page).toHaveURL(/\/admin\/pages/) | ^ 23 | 24 | const homeSection = page.locator('#home-page-editor') 25 | await expect(homeSection.getByText('Home
41. [chromium-authenticated] tests/admin-input-exceptions.spec.ts:6 admin-input-exceptions.spec.ts › admin work editor no longer exposes raw json metadata editing
   - result: timedOut, duration: 32106ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('tab', { name: 'Media & Videos' }) 7 | await page.goto('/admin/works/new') 8 | > 9 | await page.getByRole('tab', { name: 'Media & Videos' }).click() | ^ 10 | await expect(page.getByRole('heading', { name: 'Flexible Metadata' })).toBeVisible() 11 | await expect(page.getByRole('button', { name: 'Add Field' })).toBeVisible() 12 | await expect(page.locator('textarea')).toHaveCo
42. [chromium-authenticated] tests/admin-input-exceptions.spec.ts:15 admin-input-exceptions.spec.ts › admin blog editor surfaces save failure when backend rejects input
   - result: timedOut, duration: 32091ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 28 | 29 | await page.goto('/admin/blog/new') > 30 | await page.getByLabel('Title').fill(`Broken Blog ${Date.now()}`) | ^ 31 | await page.getByLabel('Tags (comma separated)').fill('qa, failure') 32 | await page.locator('.tiptap.ProseMirror').first().click() 33 | await page.keyboard.type('Body that should trigger save failure') at /work/tests/admin-input-exceptions.s
43. [chromium-authenticated] tests/admin-input-exceptions.spec.ts:40 admin-input-exceptions.spec.ts › admin resume upload rejects non-pdf files
   - result: failed, duration: 5824ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('#resume-editor').getByText('Resume Management') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('#resume-editor').getByText('Resume Management') 46 | 47 | const resumeSection = page.locator('#resume-editor') > 48 | await expect(resumeSection.getByText('Resume Management')).toBeVisible() | ^ 49 | if (await resumeSection.getByText('Resume PDF Upload
44. [chromium-authenticated] tests/admin-members.spec.ts:5 admin-members.spec.ts › admin members page lists joined users with privacy-safe fields
   - result: failed, duration: 5914ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Members' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Members' }) 6 | await page.goto('/admin/members') 7 | > 8 | await expect(page.getByRole('heading', { name: 'Members' })).toBeVisible() | ^ 9 | await expect(page.getByText(/privacy-safe/i)).toBeVisible() 10 | await expect(page.getByTestId('member-
45. [chromium-authenticated] tests/admin-menus.spec.ts:6 admin-menus.spec.ts › authenticated admin sees primary admin menus
   - result: failed, duration: 5754ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 7 | await page.goto('/admin/dashboard') 8 | > 9 | await expect(page).toHaveURL(/\/admin/) | ^ 10 | const nav = page.getByRole('navigation') 11 | await expect(nav.getByRole('link', { name: 'Dashboard' })).toBeVisible(
46. [chromium-authenticated] tests/admin-mermaid-public-independence.spec.ts:59 admin-mermaid-public-independence.spec.ts › public blog and work pages stay stable when mermaid content exists
   - result: failed, duration: 489ms
   - category: recording/batch fixtures
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 30 | }, 31 | }) > 32 | expect(response.ok()).toBeTruthy() | ^ 33 | return await response.json() as { id: string; slug: string } 34 | } 35 | at createBlog (/work/tests/admin-mermaid-public-independence.spec.ts:32:25) at /work/tests/admin-mermaid-public-independence.spec.ts:72:23
47. [chromium-authenticated] tests/admin-pages-settings.spec.ts:24 admin-pages-settings.spec.ts › admin can update site settings from admin pages
   - result: failed, duration: 5889ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/pages/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 30 | 31 | await page.goto('/admin/pages') > 32 | await expect(page).toHaveURL(/\/admin\/pages/) | ^ 33 | await page.getByRole('link', { name: 'Edit introduction page' }).click() 34 | await expect(page).toHaveU
48. [chromium-authenticated] tests/admin-pages-settings.spec.ts:56 admin-pages-settings.spec.ts › admin can update the introduction page from admin pages
   - result: timedOut, duration: 32258ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for locator('#introduction-page-editor').getByLabel('Content (HTML/Text)') 63 | await page.goto('/admin/pages') 64 | const introSection = page.locator('#introduction-page-editor') > 65 | await introSection.getByLabel('Content (HTML/Text)').fill(`<p>${introText}</p>`) | ^ 66 | await measureStep( 67 | testInfo, 68 | 'Admin introduction page save to public refresh', at /work/tests/admin-page
49. [chromium-authenticated] tests/admin-pages-settings.spec.ts:84 admin-pages-settings.spec.ts › admin can update the contact page from admin pages
   - result: timedOut, duration: 32403ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for locator('#contact-page-editor').getByLabel('Content (HTML/Text)') 91 | await page.goto('/admin/pages') 92 | const contactSection = page.locator('#contact-page-editor') > 93 | await contactSection.getByLabel('Content (HTML/Text)').fill(`<p>${contactText}</p>`) | ^ 94 | await measureStep( 95 | testInfo, 96 | 'Admin contact page save to public refresh', at /work/tests/admin-pages-setting
50. [chromium-authenticated] tests/admin-pages-validation.spec.ts:5 admin-pages-validation.spec.ts › admin pages save fails for overlong introduction title
   - result: failed, duration: 5877ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/pages/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 5 | test('admin pages save fails for overlong introduction title', async ({ page }) => { 6 | await page.goto('/admin/pages') > 7 | await expect(page).toHaveURL(/\/admin\/pages/) | ^ 8 | 9 | await page.locator(
51. [chromium-authenticated] tests/admin-regression-flow-captures.spec.ts:15 admin-regression-flow-captures.spec.ts › captures before, during, and after states for AI fixer and thumbnail fallback flows
   - result: timedOut, duration: 32099ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 44 | 45 | await page.goto('/admin/blog/new') > 46 | await page.getByLabel('Title').fill(`Capture Flow ${Date.now()}`) | ^ 47 | await page.locator('form .tiptap.ProseMirror').first().fill('rough draft before ai fix') 48 | await saveShot(page, 'ai-fixer-before-open.png') 49 | at /work/tests/admin-regression-flow-captures.spec.ts:46:34
52. [chromium-authenticated] tests/admin-regression-screenshot-capture.spec.ts:14 admin-regression-screenshot-capture.spec.ts › captures AI dialog, batch panel, and work media states
   - result: timedOut, duration: 32076ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 14 | test('captures AI dialog, batch panel, and work media states', async ({ page }) => { 15 | await page.goto('/admin/blog/new') > 16 | await page.getByLabel('Title').fill(`Screenshot Capture ${Date.now()}`) | ^ 17 | await page.locator('form .tiptap.ProseMirror').first().click() 18 | await page.keyboard.type('capture the ai dialog state') 19 | await page.getByRole
53. [chromium-authenticated] tests/admin-resume-upload.spec.ts:7 admin-resume-upload.spec.ts › admin can upload a resume pdf and public resume page exposes download
   - result: failed, duration: 5875ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('#resume-editor').getByText('Resume Management') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('#resume-editor').getByText('Resume Management') 12 | await page.goto('/admin/pages') 13 | const resumeSection = page.locator('#resume-editor') > 14 | await expect(resumeSection.getByText('Resume Management')).toBeVisible() | ^ 15 | 16 | if (await resum
54. [chromium-authenticated] tests/admin-resume-upload.spec.ts:52 admin-resume-upload.spec.ts › admin can delete the uploaded resume and clear the public resume page
   - result: failed, duration: 5850ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('#resume-editor').getByText('Resume Management') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('#resume-editor').getByText('Resume Management') 57 | await page.goto('/admin/pages') 58 | const resumeSection = page.locator('#resume-editor') > 59 | await expect(resumeSection.getByText('Resume Management')).toBeVisible() | ^ 60 | 61 | if (!(await res
55. [chromium-authenticated] tests/admin-resume-validation.spec.ts:6 admin-resume-validation.spec.ts › resume editor rejects non-pdf uploads and stays on admin page
   - result: failed, duration: 5859ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/pages/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 10 | 11 | await page.goto('/admin/pages') > 12 | await expect(page).toHaveURL(/\/admin\/pages/) | ^ 13 | const resumeSection = page.locator('#resume-editor') 14 | await expect(resumeSection.getByText('Resume M
56. [chromium-authenticated] tests/admin-search-pagination.spec.ts:178 admin-search-pagination.spec.ts › admin blog page supports title search and compact pagination controls
   - result: failed, duration: 5821ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('admin-blog-row').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('admin-blog-row').first() 180 | 181 | const rows = page.getByTestId('admin-blog-row') > 182 | await expect(rows.first()).toBeVisible() | ^ 183 | 184 | const titles = await rows.locator('td:nth-child(2) a').allTextContents() 185 | const searchTitle = titles.find(Boolea
57. [chromium-authenticated] tests/admin-search-pagination.spec.ts:212 admin-search-pagination.spec.ts › admin blog pagination adapts the row count across desktop, tablet, and mobile widths
   - result: failed, duration: 610ms
   - category: admin/editor/authenticated UI
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 61 | }, 62 | }) > 63 | expect(response.ok()).toBeTruthy() | ^ 64 | } 65 | } 66 | at seedBlogs (/work/tests/admin-search-pagination.spec.ts:63:27) at /work/tests/admin-search-pagination.spec.ts:215:3
58. [chromium-authenticated] tests/admin-search-pagination.spec.ts:227 admin-search-pagination.spec.ts › admin blog edit and delete keep the current filtered page location
   - result: failed, duration: 675ms
   - category: admin/editor/authenticated UI
   - symptom: admin API 401 fixture/setup
   - error: Error: page.evaluate: Error: Failed to seed blog "Playwright ReturnTo 1777132914006 01": 401 at eval (eval at evaluate (:290:30), <anonymous>:32:15) at async <anonymous>:316:30 at eval (eval at evaluate (:290:30), <anonymous>:32:15) at async <anonymous>:316:30 at seedBlogsWithPrefix (/work/tests/admin-search-pagination.spec.ts:68:14) at /work/tests/admin-search-pagination.spec.ts:233:9
59. [chromium-authenticated] tests/admin-search-pagination.spec.ts:308 admin-search-pagination.spec.ts › admin works page supports title search and compact pagination controls
   - result: failed, duration: 5822ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('admin-work-row').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('admin-work-row').first() 310 | 311 | const rows = page.getByTestId('admin-work-row') > 312 | await expect(rows.first()).toBeVisible() | ^ 313 | 314 | const titles = await rows.locator('td:nth-child(3) a').allTextContents() 315 | const searchTitle = titles.find(Boolea
60. [chromium-authenticated] tests/admin-search-pagination.spec.ts:344 admin-search-pagination.spec.ts › admin search ignores punctuation, spacing, and case without resetting typed input
   - result: failed, duration: 440ms
   - category: admin/editor/authenticated UI
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 152 | }, 153 | }) > 154 | expect(blogResponse.ok()).toBeTruthy() | ^ 155 | 156 | const workResponse = await request.post('/api/admin/works', { 157 | headers: { at seedNormalizedSearchFixtures (/work/tests/admin-search-pagination.spec.ts:154:29) at /work/tests/admin-search-pagination.spec.ts:345:36
61. [chromium-authenticated] tests/admin-search-pagination.spec.ts:379 admin-search-pagination.spec.ts › admin works pagination adapts the row count across desktop, tablet, and mobile widths
   - result: failed, duration: 710ms
   - category: admin/editor/authenticated UI
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 124 | }, 125 | }) > 126 | expect(response.ok()).toBeTruthy() | ^ 127 | } 128 | } 129 | at seedWorks (/work/tests/admin-search-pagination.spec.ts:126:27) at /work/tests/admin-search-pagination.spec.ts:382:3
62. [chromium-authenticated] tests/admin-search-pagination.spec.ts:394 admin-search-pagination.spec.ts › admin work edit and delete keep the current filtered page location
   - result: failed, duration: 652ms
   - category: admin/editor/authenticated UI
   - symptom: other assertion/error
   - error: Error: expect(received).toBeTruthy() Received: false 421 | }, 422 | }) > 423 | expect(response.ok()).toBeTruthy() | ^ 424 | } 425 | 426 | await page.goto(expectedPathname) at /work/tests/admin-search-pagination.spec.ts:423:27
63. [chromium-authenticated] tests/admin-search-pagination.spec.ts:499 admin-search-pagination.spec.ts › admin dashboard supports independent title search for works and blog collections
   - result: failed, duration: 5877ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('section').filter({ has: getByRole('heading', { name: 'Works' }) }).first().locator('[data-testid="works-card-link"] h3').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('section').filter({ has: getByRole('heading', { name: 'Works' }) }).first().locator('[data-testid="works-card-link"] h3').first() 506 | const blogCards = blogsSection.locat
64. [chromium-authenticated] tests/admin-site-settings-extreme-input.spec.ts:7 admin-site-settings-extreme-input.spec.ts › site settings persist mixed special-character owner and tagline values
   - result: timedOut, duration: 32102ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for locator('#ownerName') 14 | 15 | await page.goto('/admin/pages') > 16 | await page.locator('#ownerName').fill(ownerName) | ^ 17 | await page.locator('#tagline').fill(tagline) 18 | 19 | await measureStep( at /work/tests/admin-site-settings-extreme-input.spec.ts:16:36
65. [chromium-authenticated] tests/admin-work-auto-thumbnail.spec.ts:15 admin-work-auto-thumbnail.spec.ts › auto-fills thumbnail from an uploaded video when no manual thumbnail exists
   - result: timedOut, duration: 32052ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | async function fillWorkBasics(page: Page, title: string, category = 'video') { 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill(category) 12 | await page.locator('.tiptap.ProseMirror').first().fill(`Auto thumbnail coverage for ${title}`) 13 | } at fillWorkBasics (/work/test
66. [chromium-authenticated] tests/admin-work-auto-thumbnail.spec.ts:40 admin-work-auto-thumbnail.spec.ts › prefers uploaded-video auto thumbnails over youtube thumbnails
   - result: timedOut, duration: 32084ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | async function fillWorkBasics(page: Page, title: string, category = 'video') { 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill(category) 12 | await page.locator('.tiptap.ProseMirror').first().fill(`Auto thumbnail coverage for ${title}`) 13 | } at fillWorkBasics (/work/test
67. [chromium-authenticated] tests/admin-work-auto-thumbnail.spec.ts:72 admin-work-auto-thumbnail.spec.ts › falls back to the first content image when there are no videos and no explicit thumbnail
   - result: timedOut, duration: 32093ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | async function fillWorkBasics(page: Page, title: string, category = 'video') { 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill(category) 12 | await page.locator('.tiptap.ProseMirror').first().fill(`Auto thumbnail coverage for ${title}`) 13 | } at fillWorkBasics (/work/test
68. [chromium-authenticated] tests/admin-work-auto-thumbnail.spec.ts:102 admin-work-auto-thumbnail.spec.ts › auto-fills a thumbnail when the work only has a YouTube video
   - result: timedOut, duration: 32057ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | async function fillWorkBasics(page: Page, title: string, category = 'video') { 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill(category) 12 | await page.locator('.tiptap.ProseMirror').first().fill(`Auto thumbnail coverage for ${title}`) 13 | } at fillWorkBasics (/work/test
69. [chromium-authenticated] tests/admin-work-auto-thumbnail.spec.ts:125 admin-work-auto-thumbnail.spec.ts › auto-fills a thumbnail when an existing work without one gets an uploaded video
   - result: timedOut, duration: 32131ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | async function fillWorkBasics(page: Page, title: string, category = 'video') { 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill(category) 12 | await page.locator('.tiptap.ProseMirror').first().fill(`Auto thumbnail coverage for ${title}`) 13 | } at fillWorkBasics (/work/test
70. [chromium-authenticated] tests/admin-work-edit.spec.ts:7 admin-work-edit.spec.ts › admin can edit an existing work entry with mixed special input
   - result: timedOut, duration: 32090ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.getAttribute: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-work-row').first().locator('td').nth(2).getByRole('link') 16 | .nth(2) 17 | .getByRole('link') > 18 | .getAttribute('href') | ^ 19 | expect(editHref).toMatch(/\/admin\/works\//) 20 | await page.goto(editHref!) 21 | await expect(page).toHaveURL(/\/admin\/works\//) at /work/tests/admin-work-edit.spec.ts:18:6
71. [chromium-authenticated] tests/admin-work-image-upload.spec.ts:7 admin-work-image-upload.spec.ts › work editor uploads thumbnail/icon media and reuses them across admin/public views
   - result: failed, duration: 5913ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/works\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 9 | 10 | await page.goto('/admin/works/new') > 11 | await expect(page).toHaveURL(/\/admin\/works\/new/) | ^ 12 | 13 | await page.getByLabel('Title').fill(title) 14 | await page.getByLabel('Category').fill
72. [chromium-authenticated] tests/admin-work-publish.spec.ts:7 admin-work-publish.spec.ts › admin can create and publish a work that appears on public works page
   - result: failed, duration: 5885ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/works\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 9 | 10 | await page.goto('/admin/works/new') > 11 | await expect(page).toHaveURL(/\/admin\/works\/new/) | ^ 12 | await expect(page.getByLabel('Category')).toHaveValue('Uncategorized') 13 | await expect(pa
73. [chromium-authenticated] tests/admin-work-publish.spec.ts:63 admin-work-publish.spec.ts › admin can keep a work as draft and publish it later
   - result: timedOut, duration: 32139ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 65 | 66 | await page.goto('/admin/works/new') > 67 | await page.getByLabel('Title').fill(title) | ^ 68 | await page.getByLabel('Category').fill('draft') 69 | await page.getByRole('checkbox', { name: 'Published' }).uncheck() 70 | await page.locator('.tiptap.ProseMirror').first().fill(`This work starts as a draft for ${title}.`) at /work/tests/admin-work-publish.spec
74. [chromium-authenticated] tests/admin-work-publish.spec.ts:109 admin-work-publish.spec.ts › admin can toggle a published work back to draft and publish it again
   - result: timedOut, duration: 32334ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 111 | 112 | await page.goto('/admin/works/new') > 113 | await page.getByLabel('Title').fill(title) | ^ 114 | await page.getByLabel('Category').fill('toggle') 115 | await page.locator('.tiptap.ProseMirror').first().fill(`This work exercises publish toggles for ${title}.`) 116 | at /work/tests/admin-work-publish.spec.ts:113:34
75. [chromium-authenticated] tests/admin-work-special-input.spec.ts:6 admin-work-special-input.spec.ts › work editor accepts Korean and punctuation-heavy input and publishes publicly
   - result: timedOut, duration: 32232ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 9 | 10 | await page.goto('/admin/works/new') > 11 | await page.getByLabel('Title').fill(title) | ^ 12 | await page.getByLabel('Category').fill('qa-한글') 13 | await page.getByLabel('Tags (comma separated)').fill('qa,한글,!!!') 14 | await page.locator('.tiptap.ProseMirror').first().fill(content) at /work/tests/admin-work-special-input.spec.ts:11:34
76. [chromium-authenticated] tests/admin-work-validation.spec.ts:5 admin-work-validation.spec.ts › work editor exposes structured metadata controls instead of raw json
   - result: failed, duration: 5910ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/works\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 5 | test('work editor exposes structured metadata controls instead of raw json', async ({ page }) => { 6 | await page.goto('/admin/works/new') > 7 | await expect(page).toHaveURL(/\/admin\/works\/new/) | ^
77. [chromium-authenticated] tests/admin-work-validation.spec.ts:15 admin-work-validation.spec.ts › work editor shows save failure for missing required fields
   - result: failed, duration: 6049ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(page).toHaveURL(expected) failed Expected pattern: /\/admin\/works\/new/ Received string: "http://woong-prod-full-e2e-nginx-1777130558/login" Timeout: 5000ms Call log: - Expect "toHaveURL" with timeout 5000ms 9 × unexpected value "http://woong-prod-full-e2e-nginx-1777130558/login" 15 | test('work editor shows save failure for missing required fields', async ({ page }) => { 16 | await page.goto('/admin/works/new') > 17 | await expect(page).toHaveURL(/\/admin\/works\/new/) | ^ 18 | 1
78. [chromium-authenticated] tests/admin-work-video-create-flow.spec.ts:6 admin-work-video-create-flow.spec.ts › admin can create a work with staged YouTube and uploaded videos
   - result: timedOut, duration: 32225ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill('video') 12 | await page.locator('.tiptap.ProseMirror').first().fill('Work with videos') 13 | at /work/tests/admin-work-video-create-flow.spec.ts:10:34
79. [chromium-authenticated] tests/admin-work-video-drag-order.spec.ts:5 admin-work-video-drag-order.spec.ts › AF-062 saved work videos can be reordered by drag and drop and the public order updates
   - result: timedOut, duration: 32232ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill('video') 12 | await editor.fill('Drag reorder coverage.') 13 | at /work/tests/admin-work-video-drag-order.spec.ts:10:34
80. [chromium-authenticated] tests/admin-work-video-edit-flow.spec.ts:5 admin-work-video-edit-flow.spec.ts › admin can add videos inline while editing an existing work
   - result: timedOut, duration: 32218ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill('video') 12 | await editor.fill('Edit mode inline intro.') 13 | at /work/tests/admin-work-video-edit-flow.spec.ts:10:34
81. [chromium-authenticated] tests/admin-work-video-edit-flow.spec.ts:46 admin-work-video-edit-flow.spec.ts › admin can reorder saved videos and delete a removed video while editing an existing work
   - result: timedOut, duration: 32152ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 49 | 50 | await page.goto('/admin/works/new') > 51 | await page.getByLabel('Title').fill(title) | ^ 52 | await page.getByLabel('Category').fill('video') 53 | await editor.fill('Saved video order coverage.') 54 | at /work/tests/admin-work-video-edit-flow.spec.ts:51:34
82. [chromium-authenticated] tests/admin-work-video-mixed-flow.spec.ts:19 admin-work-video-mixed-flow.spec.ts › admin can create a mixed work with two youtube links, two mp4 uploads, and rich body copy
   - result: timedOut, duration: 32143ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 22 | 23 | await page.goto('/admin/works/new') > 24 | await page.getByLabel('Title').fill(title) | ^ 25 | await page.getByLabel('Category').fill('video') 26 | await page.getByLabel('Project Period').fill('2026.04') 27 | await page.getByLabel('Tags (comma separated)').fill('video, youtube, mp4, mixed') at /work/tests/admin-work-video-mixed-flow.spec.ts:24:34
83. [chromium-authenticated] tests/admin-work-video-s3-compatible.spec.ts:6 admin-work-video-s3-compatible.spec.ts › s3-compatible lane processes an uploaded MP4 as HLS and renders it publicly
   - result: timedOut, duration: 32510ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill('video') 12 | await page.locator('.tiptap.ProseMirror').first().fill('S3-backed video work') 13 | await page.locator('#work-video-upload').setInputFiles(path.resolve('tests/fixtures/sample-video.mp4')) at /work/tests/admin-wo
84. [chromium-authenticated] tests/dark-mode.spec.ts:287 dark-mode.spec.ts › login and admin dark mode › DM-14: admin dashboard uses dark cards and surfaces
   - result: failed, duration: 5846ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Dashboard' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Dashboard' }) 287 | test('DM-14: admin dashboard uses dark cards and surfaces', async ({ page }) => { 288 | await gotoWithTheme(page, '/admin/dashboard') > 289 | await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible() | ^ 2
85. [chromium-authenticated] tests/dark-mode.spec.ts:294 dark-mode.spec.ts › login and admin dark mode › DM-15: blog editor uses the dark primary button token
   - result: timedOut, duration: 32387ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByTitle('Edit') 294 | test('DM-15: blog editor uses the dark primary button token', async ({ page }) => { 295 | await gotoWithTheme(page, '/admin/blog') > 296 | await page.getByTestId('admin-blog-row').first().getByTitle('Edit').click() | ^ 297 | const saveButton = page.getByRole('button', { name: /Update Post|Create Post/i }) 298 | await expe
86. [chromium-authenticated] tests/dark-mode.spec.ts:305 dark-mode.spec.ts › login and admin dark mode › DM-16: work editor uses the dark primary button token
   - result: timedOut, duration: 32058ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-work-row').first().getByRole('link').first() 305 | test('DM-16: work editor uses the dark primary button token', async ({ page }) => { 306 | await gotoWithTheme(page, '/admin/works') > 307 | await page.getByTestId('admin-work-row').first().getByRole('link').first().click() | ^ 308 | const saveButton = page.getByRole('button', { name: /Update Work|Create Work|Create
87. [chromium-authenticated] tests/dark-mode.spec.ts:316 dark-mode.spec.ts › login and admin dark mode › DM-17: destructive buttons gain the dark hover state
   - result: failed, duration: 6012ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: 'Delete' }).first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: 'Delete' }).first() 317 | await gotoWithTheme(page, '/admin/blog') 318 | const deleteButton = page.getByRole('button', { name: 'Delete' }).first() > 319 | await expect(deleteButton).toBeVisible() | ^ 320 | await deleteButton.hover() 321 | 322
88. [chromium-authenticated] tests/e2e-admin-batch-management-journey.spec.ts:23 e2e-admin-batch-management-journey.spec.ts › E2E-009 admin can run a batch AI workflow and then bulk delete the processed items
   - result: timedOut, duration: 318921ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 300000ms exceeded. Error: locator.fill: Test timeout of 300000ms exceeded. Call log: - waiting for getByLabel('Title') 5 | async function createTemporaryBlog(page: Page, title: string) { 6 | await page.goto('/admin/blog/new') > 7 | await page.getByLabel('Title').fill(title) | ^ 8 | await page.getByLabel('Tags (comma separated)').fill('playwright, e2e, batch') 9 | await page.getByRole('checkbox', { name: 'Published' }).uncheck() 10 | at createTemporaryBlog (/work/tests/e2e-admin-b
89. [chromium-authenticated] tests/live-blog-ai-regressions.spec.ts:54 live-blog-ai-regressions.spec.ts › live AI and inline blog regressions › public blog detail save returns to the originating list page
   - result: timedOut, duration: 191533ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 180000ms exceeded. Error: locator.fill: Test timeout of 180000ms exceeded. Call log: - waiting for getByLabel('Title') 38 | async function createBlogPost(page: Page, title: string, html: string) { 39 | await page.goto('/admin/blog/new') > 40 | await page.getByLabel('Title').fill(title) | ^ 41 | await setEditorHtml(page, html) 42 | 43 | const [response] = await Promise.all([ at createBlogPost (/work/tests/live-blog-ai-regressions.spec.ts:40:34) at /work/tests/live-blog-ai-regressi
90. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:156 manual-qa-gap-coverage.spec.ts › A-6 reorder saved work videos changes public playback order
   - result: timedOut, duration: 32145ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 157 | const title = uniqueLabel('QA Reorder Work') 158 | await page.goto('/admin/works/new') > 159 | await page.getByLabel('Title').fill(title) | ^ 160 | await page.getByLabel('Category').fill('video') 161 | await fillEditor(page, 'Reorder coverage body') 162 | at /work/tests/manual-qa-gap-coverage.spec.ts:159:34
91. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:189 manual-qa-gap-coverage.spec.ts › A-12 single work delete removes the work from admin and public lists
   - result: timedOut, duration: 32114ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 74 | async function createWork(page: Page, title: string, body: string, category = 'qa') { 75 | await page.goto('/admin/works/new') > 76 | await page.getByLabel('Title').fill(title) | ^ 77 | await page.getByLabel('Category').fill(category) 78 | await fillEditor(page, body) 79 | at createWork (/work/tests/manual-qa-gap-coverage.spec.ts:76:34) at /work/tests/manual-q
92. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:206 manual-qa-gap-coverage.spec.ts › B-5 single blog delete removes the post from admin and public lists
   - result: timedOut, duration: 32129ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 53 | async function createBlog(page: Page, title: string, body: string, tags = 'playwright, qa') { 54 | await page.goto('/admin/blog/new') > 55 | await page.getByLabel('Title').fill(title) | ^ 56 | await page.getByLabel('Tags (comma separated)').fill(tags) 57 | const editor = await fillEditor(page, body) 58 | at createBlog (/work/tests/manual-qa-gap-coverage.spec.t
93. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:223 manual-qa-gap-coverage.spec.ts › C-1 basic formatting renders publicly after save
   - result: timedOut, duration: 32115ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 40 | async function publishBlogWithHtml(page: Page, title: string, html: string) { 41 | await page.goto('/admin/blog/new') > 42 | await page.getByLabel('Title').fill(title) | ^ 43 | await setEditorHtml(page, html) 44 | 45 | const [saveResponse] = await Promise.all([ at publishBlogWithHtml (/work/tests/manual-qa-gap-coverage.spec.ts:42:34) at /work/tests/manual-qa-g
94. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:238 manual-qa-gap-coverage.spec.ts › C-2 link insertion renders publicly after save
   - result: timedOut, duration: 32122ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 40 | async function publishBlogWithHtml(page: Page, title: string, html: string) { 41 | await page.goto('/admin/blog/new') > 42 | await page.getByLabel('Title').fill(title) | ^ 43 | await setEditorHtml(page, html) 44 | 45 | const [saveResponse] = await Promise.all([ at publishBlogWithHtml (/work/tests/manual-qa-gap-coverage.spec.ts:42:34) at /work/tests/manual-qa-g
95. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:249 manual-qa-gap-coverage.spec.ts › C-7 code block renders publicly after save
   - result: timedOut, duration: 32066ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 40 | async function publishBlogWithHtml(page: Page, title: string, html: string) { 41 | await page.goto('/admin/blog/new') > 42 | await page.getByLabel('Title').fill(title) | ^ 43 | await setEditorHtml(page, html) 44 | 45 | const [saveResponse] = await Promise.all([ at publishBlogWithHtml (/work/tests/manual-qa-gap-coverage.spec.ts:42:34) at /work/tests/manual-qa-g
96. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:260 manual-qa-gap-coverage.spec.ts › C-10 selecting editor text reveals the bubble menu
   - result: timedOut, duration: 32149ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 260 | test('C-10 selecting editor text reveals the bubble menu', async ({ page }) => { 261 | await page.goto('/admin/blog/new') > 262 | await page.getByLabel('Title').fill(uniqueLabel('QA Bubble Menu')) | ^ 263 | await fillEditor(page, 'Bubble menu text') 264 | await page.evaluate(() => { 265 | const editorNode = document.querySelector('.tiptap.ProseMirror') at /wo
97. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:284 manual-qa-gap-coverage.spec.ts › C-3 drag-drop image upload inserts an image into blog content
   - result: timedOut, duration: 32127ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 285 | const title = uniqueLabel('QA Drag Drop Image') 286 | await page.goto('/admin/blog/new') > 287 | await page.getByLabel('Title').fill(title) | ^ 288 | const editor = await fillEditor(page, 'Drag drop image body') 289 | const dataTransfer = await buildDataTransferHandle(page, 'tests/fixtures/avatar.png', 'avatar.png', 'image/png') 290 | at /work/tests/manual-qa
98. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:314 manual-qa-gap-coverage.spec.ts › C-4 paste image upload inserts an image into blog content
   - result: timedOut, duration: 32299ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 315 | const title = uniqueLabel('QA Paste Image') 316 | await page.goto('/admin/blog/new') > 317 | await page.getByLabel('Title').fill(title) | ^ 318 | await fillEditor(page, 'Paste image body') 319 | await Promise.all([ 320 | page.waitForResponse((res) => res.url().includes('/api/uploads') && res.request().method() === 'POST' && res.ok()), at /work/tests/manual-qa
99. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:340 manual-qa-gap-coverage.spec.ts › C-5 duplicate work video embed insertion is prevented
   - result: timedOut, duration: 32222ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 341 | const title = uniqueLabel('QA Duplicate Video Embed') 342 | await page.goto('/admin/works/new') > 343 | await page.getByLabel('Title').fill(title) | ^ 344 | await page.getByLabel('Category').fill('video') 345 | await fillEditor(page, 'Video embed body') 346 | await page.getByLabel('YouTube URL or ID').fill('dQw4w9WgXcQ') at /work/tests/manual-qa-gap-coverage.
100. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:370 manual-qa-gap-coverage.spec.ts › C-6 slash command inserts a heading block that renders publicly
   - result: timedOut, duration: 32179ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 371 | const title = uniqueLabel('QA Slash Command') 372 | await page.goto('/admin/blog/new') > 373 | await page.getByLabel('Title').fill(title) | ^ 374 | await fillEditor(page, '/h2') 375 | await expect(page.getByText('Heading 2')).toBeVisible() 376 | await page.getByText('Heading 2').click() at /work/tests/manual-qa-gap-coverage.spec.ts:373:34
101. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:389 manual-qa-gap-coverage.spec.ts › C-8 HTML widget renders through the public interactive renderer
   - result: failed, duration: 799ms
   - category: admin/editor/authenticated UI
   - symptom: other assertion/error
   - error: Error: page.evaluate: Error at eval (eval at evaluate (:290:30), <anonymous>:31:13) at async <anonymous>:316:30 at eval (eval at evaluate (:290:30), <anonymous>:31:13) at async <anonymous>:316:30 at /work/tests/manual-qa-gap-coverage.spec.ts:394:14
102. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:432 manual-qa-gap-coverage.spec.ts › C-9 Three.js block renders a canvas publicly
   - result: timedOut, duration: 32524ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 433 | const title = uniqueLabel('QA Three Block') 434 | await page.goto('/admin/works/new') > 435 | await page.getByLabel('Title').fill(title) | ^ 436 | await page.getByLabel('Category').fill('3d') 437 | await fillEditor(page, 'Three block body') 438 | await page.locator('[title="Insert 3D Model"]').click() at /work/tests/manual-qa-gap-coverage.spec.ts:435:34
103. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:450 manual-qa-gap-coverage.spec.ts › C-11 editor content persists and reloads into the edit surface
   - result: timedOut, duration: 32326ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 53 | async function createBlog(page: Page, title: string, body: string, tags = 'playwright, qa') { 54 | await page.goto('/admin/blog/new') > 55 | await page.getByLabel('Title').fill(title) | ^ 56 | await page.getByLabel('Tags (comma separated)').fill(tags) 57 | const editor = await fillEditor(page, body) 58 | at createBlog (/work/tests/manual-qa-gap-coverage.spec.t
104. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:468 manual-qa-gap-coverage.spec.ts › F-3 and F-4 inline page editors can save introduction and contact content in place
   - result: timedOut, duration: 31841ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '소개글 수정' }) 471 | 472 | await page.goto('/introduction') > 473 | await page.getByRole('button', { name: '소개글 수정' }).click() | ^ 474 | await page.getByLabel('Content (HTML/Text)').fill(`<p>${introText}</p>`) 475 | await Promise.all([ 476 | page.waitForResponse((res) => res.url().includes('/api/admin/pages') && res.request().method() === 'PUT' && res.ok()), 
105. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:491 manual-qa-gap-coverage.spec.ts › F-5 and F-6 inline create flows can create works and blog posts from public pages
   - result: timedOut, duration: 31961ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '새 작업 쓰기' }) 494 | 495 | await page.goto('/works') > 496 | await page.getByRole('button', { name: '새 작업 쓰기' }).click() | ^ 497 | await page.getByLabel('Title').fill(workTitle) 498 | await page.getByLabel('Category').fill('inline') 499 | await fillEditor(page, 'Inline work create body') at /work/tests/manual-qa-gap-coverage.spec.ts:496:55
106. [chromium-authenticated] tests/manual-qa-gap-coverage.spec.ts:530 manual-qa-gap-coverage.spec.ts › H-5 very long body content saves and renders correctly
   - result: timedOut, duration: 32497ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 53 | async function createBlog(page: Page, title: string, body: string, tags = 'playwright, qa') { 54 | await page.goto('/admin/blog/new') > 55 | await page.getByLabel('Title').fill(title) | ^ 56 | await page.getByLabel('Tags (comma separated)').fill(tags) 57 | const editor = await fillEditor(page, body) 58 | at createBlog (/work/tests/manual-qa-gap-coverage.spec.t
107. [chromium-authenticated] tests/public-blog-detail-inline-edit.spec.ts:7 public-blog-detail-inline-edit.spec.ts › admin can edit a public blog detail inline and return to the originating blog page
   - result: failed, duration: 6175ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: '글 수정' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: '글 수정' }) 13 | await page.locator('a[href^="/blog/"]').first().click() 14 | > 15 | await expect(page.getByRole('button', { name: '글 수정' })).toBeVisible() | ^ 16 | await page.getByRole('button', { name: '글 수정' }).click() 17 | 18 | const saveButton = pag
108. [chromium-authenticated] tests/public-footer-social.spec.ts:13 public-footer-social.spec.ts › PF-084 footer renders only the configured social icons
   - result: timedOut, duration: 33458ms
   - category: admin/editor/authenticated UI
   - symptom: 33s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.inputValue: Test timeout of 30000ms exceeded. Call log: - waiting for locator('#instagramUrl') 19 | await page.goto('/admin/pages') 20 | > 21 | const originalInstagram = await instagramInput.inputValue() | ^ 22 | const originalTwitter = await twitterInput.inputValue() 23 | const originalLinkedIn = await linkedinInput.inputValue() 24 | const originalGitHub = await githubInput.inputValue() at /work/tests/public-footer-social.spec.ts:21:50
109. [chromium-authenticated] tests/public-inline-editors-unsaved-warning.spec.ts:27 public-inline-editors-unsaved-warning.spec.ts › public blog inline editor clears beforeunload after save
   - result: timedOut, duration: 98822ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 90000ms exceeded. Error: locator.fill: Test timeout of 90000ms exceeded. Call log: - waiting for getByLabel('Title') 12 | async function createBlogForInlineFlow(page: import('./helpers/performance-test').Page, title: string, body: string) { 13 | await page.goto('/admin/blog/new') > 14 | await page.getByLabel('Title').fill(title) | ^ 15 | await page.locator('.tiptap.ProseMirror').first().fill(body) 16 | 17 | const [response] = await Promise.all([ at createBlogForInlineFlow (/work/
110. [chromium-authenticated] tests/public-inline-editors-unsaved-warning.spec.ts:62 public-inline-editors-unsaved-warning.spec.ts › public work inline editor clears beforeunload after save
   - result: timedOut, duration: 96831ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 90000ms exceeded. Error: locator.click: Test timeout of 90000ms exceeded. Call log: - waiting for getByRole('button', { name: '작업 수정' }) 72 | await page.getByTestId('work-card').first().click() 73 | await expect(page).toHaveURL(/\/works\/[^/?#]+\?returnTo=/) > 74 | await page.getByRole('button', { name: '작업 수정' }).click() | ^ 75 | 76 | await page.getByLabel('Title').fill(updatedTitle) 77 | await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('func
111. [chromium-authenticated] tests/public-inline-editors.spec.ts:5 public-inline-editors.spec.ts › introduction and contact inline editors open in place and can be closed with back buttons
   - result: timedOut, duration: 31486ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '소개글 수정' }) 5 | test('introduction and contact inline editors open in place and can be closed with back buttons', async ({ page }) => { 6 | await page.goto('/introduction') > 7 | await page.getByRole('button', { name: '소개글 수정' }).click() | ^ 8 | await expect(page.getByRole('heading', { name: 'Introduction Inline Editor' }).first()).toBeVisible() 9 | await 
112. [chromium-authenticated] tests/public-inline-editors.spec.ts:22 public-inline-editors.spec.ts › works and study inline create panels open in place and can be closed
   - result: failed, duration: 5766ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: '새 작업 쓰기' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: '새 작업 쓰기' }) 22 | test('works and study inline create panels open in place and can be closed', async ({ page }) => { 23 | await page.goto('/works') > 24 | await expect(page.getByRole('button', { name: '새 작업 쓰기' })).toBeVisible() | ^ 25 | await page.
113. [chromium-authenticated] tests/public-inline-editors.spec.ts:40 public-inline-editors.spec.ts › resume page exposes inline PDF upload shell for admins
   - result: timedOut, duration: 31536ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '이력서 PDF 업로드' }) 40 | test('resume page exposes inline PDF upload shell for admins', async ({ page }) => { 41 | await page.goto('/resume') > 42 | await page.getByRole('button', { name: '이력서 PDF 업로드' }).click() | ^ 43 | await expect(page.getByRole('heading', { name: 'Resume Inline Upload' })).toBeVisible() 44 | await expect(page.getByText(/Upload your lates
114. [chromium-authenticated] tests/public-work-detail-inline-edit.spec.ts:7 public-work-detail-inline-edit.spec.ts › admin can edit a public work detail inline and return to the originating works page
   - result: failed, duration: 6210ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: '작업 수정' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: '작업 수정' }) 13 | await page.locator('a[href^="/works/"]').first().click() 14 | > 15 | await expect(page.getByRole('button', { name: '작업 수정' })).toBeVisible() | ^ 16 | await page.getByRole('button', { name: '작업 수정' }).click() 17 | 18 | const saveButton 
115. [chromium-authenticated] tests/public-work-videos.spec.ts:6 public-work-videos.spec.ts › public work detail renders uploaded and youtube videos when present
   - result: timedOut, duration: 31880ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 7 | const title = `Public Video Flow ${Date.now()}` 8 | await page.goto('/admin/works/new') > 9 | await page.getByLabel('Title').fill(title) | ^ 10 | await page.getByLabel('Category').fill('video') 11 | await page.locator('.tiptap.ProseMirror').first().fill('Public video work') 12 | await page.getByLabel('YouTube URL or ID').fill('dQw4w9WgXcQ') at /work/tests/publi
116. [chromium-authenticated] tests/public-work-videos.spec.ts:29 public-work-videos.spec.ts › PF-041 public work detail lets visitors play and pause an uploaded video
   - result: timedOut, duration: 32061ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 30 | const title = `Public Video Playback ${Date.now()}` 31 | await page.goto('/admin/works/new') > 32 | await page.getByLabel('Title').fill(title) | ^ 33 | await page.getByLabel('Category').fill('video') 34 | await page.locator('.tiptap.ProseMirror').first().fill('Playback coverage for an uploaded public video') 35 | await page.locator('#work-video-upload').setInp
117. [chromium-authenticated] tests/public-work-videos.spec.ts:82 public-work-videos.spec.ts › PF-043 public work detail keeps public video render order aligned with saved sort order
   - result: timedOut, duration: 31911ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 83 | const title = `Public Video Order ${Date.now()}` 84 | await page.goto('/admin/works/new') > 85 | await page.getByLabel('Title').fill(title) | ^ 86 | await page.getByLabel('Category').fill('video') 87 | await page.locator('.tiptap.ProseMirror').first().fill('Render order coverage for public work videos') 88 | at /work/tests/public-work-videos.spec.ts:85:34
118. [chromium-authenticated] tests/public-work-videos.spec.ts:118 public-work-videos.spec.ts › PF-044 public work detail exposes hover preview near the native progress region and desktop resize modes for uploaded videos
   - result: timedOut, duration: 31816ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 119 | const title = `Public Video Preview ${Date.now()}` 120 | await page.goto('/admin/works/new') > 121 | await page.getByLabel('Title').fill(title) | ^ 122 | await page.getByLabel('Category').fill('video') 123 | await page.locator('.tiptap.ProseMirror').first().fill('Preview coverage for public work videos') 124 | await page.locator('#work-video-upload').setInput
119. [chromium-authenticated] tests/public-work-videos.spec.ts:202 public-work-videos.spec.ts › PF-045 mobile public work detail disables timeline preview on touch devices
   - result: timedOut, duration: 31813ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 203 | const title = `Public Video Mobile Preview ${Date.now()}` 204 | await page.goto('/admin/works/new') > 205 | await page.getByLabel('Title').fill(title) | ^ 206 | await page.getByLabel('Category').fill('video') 207 | await page.locator('.tiptap.ProseMirror').first().fill('Mobile preview disable coverage for public work videos') 208 | await page.locator('#work-v
120. [chromium-authenticated] tests/regression-screenshot-capture.spec.ts:31 regression-screenshot-capture.spec.ts › admin regression captures › captures AI dialog, batch panel, and work thumbnail states
   - result: timedOut, duration: 31965ms
   - category: other
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 33 | 34 | await page.goto('/admin/blog/new') > 35 | await page.getByLabel('Title').fill(`Screenshot Capture ${Date.now()}`) | ^ 36 | await page.locator('form .tiptap.ProseMirror').first().click() 37 | await page.keyboard.type('capture the ai dialog state') 38 | await page.getByRole('button', { name: 'AI Content Fixer' }).click() at /work/tests/regression-screenshot
121. [chromium-authenticated] tests/renovation-0416-regression.spec.ts:70 renovation-0416-regression.spec.ts › 0416 Study search supports title and content modes through the URL
   - result: failed, duration: 453ms
   - category: admin/editor/authenticated UI
   - symptom: admin API 401 fixture/setup
   - error: Error: page.evaluate: Error: Failed to create searchable post: 401 at eval (eval at evaluate (:290:30), <anonymous>:30:13) at async <anonymous>:316:30 at eval (eval at evaluate (:290:30), <anonymous>:30:13) at async <anonymous>:316:30 at /work/tests/renovation-0416-regression.spec.ts:76:14
122. [chromium-authenticated] tests/renovation-0416-regression.spec.ts:117 renovation-0416-regression.spec.ts › 0416 admin home edits read back in admin and public home
   - result: timedOut, duration: 31810ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for locator('#headline') 124 | 125 | await page.goto('/admin/pages') > 126 | await page.locator('#headline').fill(headline) | ^ 127 | await page.locator('#introText').fill(intro) 128 | 129 | await Promise.all([ at /work/tests/renovation-0416-regression.spec.ts:126:35
123. [chromium-authenticated] tests/renovation-0416-regression.spec.ts:151 renovation-0416-regression.spec.ts › 0416 batch AI jobs refresh only on explicit user action
   - result: timedOut, duration: 31844ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: 'Batch AI Fix' }) 192 | 193 | await page.goto('/admin/blog') > 194 | await page.getByRole('button', { name: 'Batch AI Fix' }).click() | ^ 195 | await expect(page.getByTestId('admin-blog-batch-ai-panel')).toBeVisible() 196 | await expect.poll(() => listCalls).toBe(1) 197 | at /work/tests/renovation-0416-regression.spec.ts:194:60
124. [chromium-authenticated] tests/ui-admin-blog-excerpt.spec.ts:5 ui-admin-blog-excerpt.spec.ts › BlogEditor exposes an excerpt field with a live character counter
   - result: failed, duration: 5666ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByLabel('Excerpt') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByLabel('Excerpt') 8 | await page.goto('/admin/blog/new') 9 | > 10 | await expect(page.getByLabel('Excerpt')).toBeVisible() | ^ 11 | await expect(page.getByRole('button', { name: 'Create Post' })).toBeDisabled() 12 | await expect(page.getByRole('button', { name: 'Save changes from top action 
125. [chromium-authenticated] tests/ui-admin-blog-published-position.spec.ts:5 ui-admin-blog-published-position.spec.ts › Published checkbox stays above the blog editor body area
   - result: failed, duration: 5753ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('checkbox', { name: 'Published' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('checkbox', { name: 'Published' }) 9 | const editor = page.locator('.tiptap.ProseMirror').first() 10 | > 11 | await expect(publishedCheckbox).toBeVisible() | ^ 12 | await expect(editor).toBeVisible() 13 | 14 | const checkboxBox = await publishedCheckbox.boundingB
126. [chromium-authenticated] tests/ui-admin-copy-cleanup.spec.ts:5 ui-admin-copy-cleanup.spec.ts › admin blog list uses concise management copy
   - result: failed, duration: 5782ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Blog Posts' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Blog Posts' }) 6 | await page.goto('/admin/blog') 7 | > 8 | await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible() | ^ 9 | await expect(page.getByText('Manage all blog posts. Click a title to edit.', { exact: true })).to
127. [chromium-authenticated] tests/ui-admin-copy-cleanup.spec.ts:13 ui-admin-copy-cleanup.spec.ts › admin works list uses concise management copy
   - result: failed, duration: 5749ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Works' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Works' }) 14 | await page.goto('/admin/works') 15 | > 16 | await expect(page.getByRole('heading', { name: 'Works' })).toBeVisible() | ^ 17 | await expect(page.getByText('Manage all portfolio works.', { exact: true })).toBeVisible() 18 | await expec
128. [chromium-authenticated] tests/ui-admin-editor-keyboard-save.spec.ts:19 ui-admin-editor-keyboard-save.spec.ts › blog editor saves from the keyboard shortcut
   - result: timedOut, duration: 31802ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.getAttribute: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().locator('a[href^="/admin/blog/"]').first() 11 | .locator(`a[href^="${hrefPrefix}"]`) 12 | .first() > 13 | .getAttribute('href') | ^ 14 | 15 | expect(href).toBeTruthy() 16 | return href! at getFirstAdminEditHref (/work/tests/ui-admin-editor-keyboard-save.spec.ts:13:6) at /work/tests/ui-admin-editor-keyboard-save.spec.ts:23:26
129. [chromium-authenticated] tests/ui-admin-editor-keyboard-save.spec.ts:42 ui-admin-editor-keyboard-save.spec.ts › work editor saves from the keyboard shortcut
   - result: timedOut, duration: 31929ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.getAttribute: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-work-row').first().locator('a[href^="/admin/works/"]').first() 11 | .locator(`a[href^="${hrefPrefix}"]`) 12 | .first() > 13 | .getAttribute('href') | ^ 14 | 15 | expect(href).toBeTruthy() 16 | return href! at getFirstAdminEditHref (/work/tests/ui-admin-editor-keyboard-save.spec.ts:13:6) at /work/tests/ui-admin-editor-keyboard-save.spec.ts:46:26
130. [chromium-authenticated] tests/ui-admin-footer-social-size.spec.ts:5 ui-admin-footer-social-size.spec.ts › VA-304 footer social icons keep 44px touch targets once configured
   - result: failed, duration: 5722ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByLabel('Twitter URL') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByLabel('Twitter URL') 7 | 8 | const twitterInput = page.getByLabel('Twitter URL') > 9 | await expect(twitterInput).toBeVisible() | ^ 10 | await twitterInput.fill('https://twitter.com/example') 11 | await page.getByRole('button', { name: 'Save Changes' }).first().click() 12 | at /work/tes
131. [chromium-authenticated] tests/ui-admin-form-errors.spec.ts:5 ui-admin-form-errors.spec.ts › WQ-009 blog editor exposes save errors through an inline live region
   - result: timedOut, duration: 31801ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 18 | 19 | await page.goto('/admin/blog/new') > 20 | await page.getByLabel('Title').fill(`A11y Blog ${Date.now()}`) | ^ 21 | await page.locator('.tiptap.ProseMirror').first().click() 22 | await page.keyboard.type('Trigger accessible blog form error.') 23 | await page.getByRole('button', { name: 'Create Post' }).click() at /work/tests/ui-admin-form-errors.spec.ts:20:
132. [chromium-authenticated] tests/ui-admin-form-errors.spec.ts:31 ui-admin-form-errors.spec.ts › WQ-009 work editor exposes save errors through an inline live region
   - result: timedOut, duration: 31931ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 44 | 45 | await page.goto('/admin/works/new') > 46 | await page.getByLabel('Title').fill(`A11y Work ${Date.now()}`) | ^ 47 | await page.getByRole('button', { name: 'Create Work' }).click() 48 | 49 | const alert = page.getByTestId('admin-work-form-error') at /work/tests/ui-admin-form-errors.spec.ts:46:34
133. [chromium-authenticated] tests/ui-admin-notion-autosave-info.spec.ts:37 ui-admin-notion-autosave-info.spec.ts › AF-042 autosave status waits for the autosave interval, then shows Saving and Saved after notion content changes
   - result: failed, duration: 449ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-autosave-info.spec.ts:39:16
134. [chromium-authenticated] tests/ui-admin-notion-autosave-info.spec.ts:81 ui-admin-notion-autosave-info.spec.ts › AF-042 autosave revalidation is throttled while explicit metadata save revalidates immediately
   - result: failed, duration: 445ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-autosave-info.spec.ts:84:16
135. [chromium-authenticated] tests/ui-admin-notion-autosave-info.spec.ts:131 ui-admin-notion-autosave-info.spec.ts › AF-042 autosave status shows Error when notion autosave fails
   - result: failed, duration: 458ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-autosave-info.spec.ts:133:16
136. [chromium-authenticated] tests/ui-admin-notion-autosave-info.spec.ts:170 ui-admin-notion-autosave-info.spec.ts › AF-045 notion document info panel shows timestamps and slug for the selected post
   - result: failed, duration: 450ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-autosave-info.spec.ts:171:19
137. [chromium-authenticated] tests/ui-admin-notion-autosave-info.spec.ts:211 ui-admin-notion-autosave-info.spec.ts › AF-046 Ctrl+S saves notion content and metadata immediately
   - result: failed, duration: 457ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-autosave-info.spec.ts:213:16
138. [chromium-authenticated] tests/ui-admin-notion-client-switch.spec.ts:5 ui-admin-notion-client-switch.spec.ts › switching documents in notion view keeps the editor visible while changing the active document
   - result: timedOut, duration: 31868ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-library-trigger') 6 | await page.goto('/admin/blog/notion') 7 | > 8 | await page.getByTestId('notion-library-trigger').click() | ^ 9 | const listItems = page.getByTestId('notion-blog-list-item') 10 | await page.waitForTimeout(500) 11 | const itemCount = await listItems.count() at /work/tests/ui-admin-notion-client-switch.spec.ts:8:52
139. [chromium-authenticated] tests/ui-admin-notion-client-switch.spec.ts:37 ui-admin-notion-client-switch.spec.ts › selected notion document persists after reload via url state
   - result: timedOut, duration: 31903ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-library-trigger') 38 | await page.goto('/admin/blog/notion') 39 | > 40 | await page.getByTestId('notion-library-trigger').click() | ^ 41 | const listItems = page.getByTestId('notion-blog-list-item') 42 | await page.waitForTimeout(500) 43 | const itemCount = await listItems.count() at /work/tests/ui-admin-notion-client-switch.spec.ts:40:52
140. [chromium-authenticated] tests/ui-admin-notion-doc-info-toggle.spec.ts:7 ui-admin-notion-doc-info-toggle.spec.ts › doc info toggle off expands the notion editor area
   - result: failed, duration: 5841ms
   - category: admin notion view
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('notion-editor-area') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('notion-editor-area') 10 | 11 | const editorArea = page.getByTestId('notion-editor-area') > 12 | await expect(editorArea).toBeVisible() | ^ 13 | await expect(page.getByTestId('notion-doc-info')).toBeVisible() 14 | 15 | const beforeWidth = await measureWidth(page, 'notion-
141. [chromium-authenticated] tests/ui-admin-notion-doc-info-toggle.spec.ts:23 ui-admin-notion-doc-info-toggle.spec.ts › doc info toggle on restores the side panel
   - result: timedOut, duration: 31909ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-doc-info-toggle') 24 | await page.goto('/admin/blog/notion') 25 | > 26 | await page.getByTestId('notion-doc-info-toggle').click() | ^ 27 | await expect(page.getByTestId('notion-doc-info')).toBeHidden() 28 | 29 | await page.getByTestId('notion-doc-info-toggle').click() at /work/tests/ui-admin-notion-doc-info-toggle.spec.ts:26:52
142. [chromium-authenticated] tests/ui-admin-notion-hint-dismiss.spec.ts:3 ui-admin-notion-hint-dismiss.spec.ts › capability hint can be dismissed and stays hidden after reload
   - result: failed, duration: 5757ms
   - category: admin notion view
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('tiptap-capability-hint').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('tiptap-capability-hint').first() 5 | 6 | const hint = page.getByTestId('tiptap-capability-hint').first() > 7 | await expect(hint).toBeVisible() | ^ 8 | await page.getByRole('button', { name: /close hint/i }).click() 9 | await expect(hint).toBeHidden() 10 | at
143. [chromium-authenticated] tests/ui-admin-notion-hint-dismiss.spec.ts:15 ui-admin-notion-hint-dismiss.spec.ts › clearing localStorage restores the capability hint
   - result: timedOut, duration: 31915ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: /close hint/i }) 16 | await page.goto('/admin/blog/notion') 17 | > 18 | await page.getByRole('button', { name: /close hint/i }).click() | ^ 19 | await expect(page.getByTestId('tiptap-capability-hint').first()).toBeHidden() 20 | 21 | await page.evaluate(() => { at /work/tests/ui-admin-notion-hint-dismiss.spec.ts:18:59
144. [chromium-authenticated] tests/ui-admin-notion-library-search.spec.ts:3 ui-admin-notion-library-search.spec.ts › library search filters visible notion documents
   - result: timedOut, duration: 31909ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-library-trigger') 4 | await page.goto('/admin/blog/notion') 5 | > 6 | await page.getByTestId('notion-library-trigger').click() | ^ 7 | const searchInput = page.getByPlaceholder('Search posts...') 8 | await expect(searchInput).toBeVisible() 9 | at /work/tests/ui-admin-notion-library-search.spec.ts:6:52
145. [chromium-authenticated] tests/ui-admin-notion-library-search.spec.ts:25 ui-admin-notion-library-search.spec.ts › clearing search restores the full notion document list
   - result: timedOut, duration: 31948ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-library-trigger') 26 | await page.goto('/admin/blog/notion') 27 | > 28 | await page.getByTestId('notion-library-trigger').click() | ^ 29 | const searchInput = page.getByPlaceholder('Search posts...') 30 | const items = page.getByTestId('notion-blog-list-item') 31 | const initialCount = await items.count() at /work/tests/ui-admin-notion-library-search.spec.ts:28:52
146. [chromium-authenticated] tests/ui-admin-notion-library-sheet.spec.ts:3 ui-admin-notion-library-sheet.spec.ts › notion view does not expose the library panel by default
   - result: failed, duration: 5814ms
   - category: admin notion view
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Blog Notion View' }).first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Blog Notion View' }).first() 4 | await page.goto('/admin/blog/notion') 5 | > 6 | await expect(page.getByRole('heading', { name: 'Blog Notion View' }).first()).toBeVisible() | ^ 7 | await expect(page.getByTestId('notion-library-tr
147. [chromium-authenticated] tests/ui-admin-notion-library-sheet.spec.ts:11 ui-admin-notion-library-sheet.spec.ts › library button opens sheet and exposes blog list
   - result: timedOut, duration: 32024ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-library-trigger') 12 | await page.goto('/admin/blog/notion') 13 | > 14 | await page.getByTestId('notion-library-trigger').click() | ^ 15 | await expect(page.getByTestId('notion-library-sheet')).toBeVisible() 16 | await page.waitForTimeout(500) 17 | const listItems = page.getByTestId('notion-blog-list-item') at /work/tests/ui-admin-notion-library-sheet.spec.ts:14:5
148. [chromium-authenticated] tests/ui-admin-notion-library-sheet.spec.ts:23 ui-admin-notion-library-sheet.spec.ts › selecting a document closes the sheet and keeps editor visible
   - result: timedOut, duration: 31879ms
   - category: admin notion view
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('notion-library-trigger') 24 | await page.goto('/admin/blog/notion') 25 | > 26 | await page.getByTestId('notion-library-trigger').click() | ^ 27 | await page.waitForTimeout(500) 28 | const listItems = page.getByTestId('notion-blog-list-item') 29 | const itemCount = await listItems.count() at /work/tests/ui-admin-notion-library-sheet.spec.ts:26:52
149. [chromium-authenticated] tests/ui-admin-notion-library-sheet.spec.ts:38 ui-admin-notion-library-sheet.spec.ts › editor shell keeps at least 80 percent of the viewport width on desktop
   - result: failed, duration: 5812ms
   - category: admin notion view
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('notion-editor-shell') Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('notion-editor-shell') 41 | 42 | const editorShell = page.getByTestId('notion-editor-shell') > 43 | await expect(editorShell).toBeVisible() | ^ 44 | 45 | await expect.poll( 46 | () => editorShell.evaluate((element) => Math.round(element.getBoundingClientRect().width)), a
150. [chromium-authenticated] tests/ui-admin-notion-visual-state.spec.ts:7 ui-admin-notion-visual-state.spec.ts › VA-241 selected notion document stays visually highlighted inside the library sheet
   - result: failed, duration: 456ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-visual-state.spec.ts:8:16
151. [chromium-authenticated] tests/ui-admin-notion-visual-state.spec.ts:33 ui-admin-notion-visual-state.spec.ts › VA-242 notion save-state chip changes visual treatment across saved and error states
   - result: failed, duration: 449ms
   - category: admin notion view
   - symptom: admin API 401 fixture/setup
   - error: Error: Failed to create blog fixture: 401 at helpers/content-fixtures.ts:70 68 | 69 | if (!response.ok()) { > 70 | throw new Error(`Failed to create blog fixture: ${response.status()} ${await response.text()}`) | ^ 71 | } 72 | 73 | const payload = await response.json() as { id: string; slug: string } at createBlogFixture (/work/tests/helpers/content-fixtures.ts:70:11) at /work/tests/ui-admin-notion-visual-state.spec.ts:35:16
152. [chromium-authenticated] tests/ui-admin-save-btn.spec.ts:5 ui-admin-save-btn.spec.ts › Blog and Work primary save buttons no longer use the legacy hover-scale styling
   - result: failed, duration: 5790ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: 'Create Post' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: 'Create Post' }) 6 | await page.goto('/admin/blog/new') 7 | const blogSaveButton = page.getByRole('button', { name: 'Create Post' }) > 8 | await expect(blogSaveButton).toBeVisible() | ^ 9 | await expect(blogSaveButton).toHaveAttribute('data-vari
153. [chromium-authenticated] tests/ui-admin-semantic-colors.spec.ts:7 ui-admin-semantic-colors.spec.ts › VA-005 success, warning, and error states keep distinct semantic color treatments
   - result: timedOut, duration: 63741ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 60000ms exceeded. Error: locator.fill: Test timeout of 60000ms exceeded. Call log: - waiting for getByLabel('Title') 9 | 10 | await page.goto('/admin/blog/new') > 11 | await page.getByLabel('Title').fill(draftTitle) | ^ 12 | await page.getByRole('checkbox', { name: 'Published' }).uncheck() 13 | await page.locator('form .tiptap.ProseMirror').first().click() 14 | await page.keyboard.type('Draft body for semantic color verification.') at /work/tests/ui-admin-semantic-colors.spec.ts:
154. [chromium-authenticated] tests/ui-admin-sidebar-active.spec.ts:3 ui-admin-sidebar-active.spec.ts › dashboard nav item is visually active on /admin/dashboard
   - result: failed, duration: 5838ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('link', { name: /dashboard/i }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('link', { name: /dashboard/i }) 5 | 6 | const dashboardLink = page.getByRole('link', { name: /dashboard/i }) > 7 | await expect(dashboardLink).toBeVisible() | ^ 8 | await expect(dashboardLink).toHaveClass(/font-semibold/) 9 | await expect(dashboardLink).toHaveAttrib
155. [chromium-authenticated] tests/ui-admin-sidebar-active.spec.ts:12 ui-admin-sidebar-active.spec.ts › blog nav item is visually active on /admin/blog
   - result: failed, duration: 5831ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('link', { name: /^blog$/i }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('link', { name: /^blog$/i }) 14 | 15 | const blogLink = page.getByRole('link', { name: /^blog$/i }) > 16 | await expect(blogLink).toBeVisible() | ^ 17 | await expect(blogLink).toHaveClass(/font-semibold/) 18 | await expect(blogLink).toHaveAttribute('aria-current', 'pag
156. [chromium-authenticated] tests/ui-admin-sidebar-active.spec.ts:21 ui-admin-sidebar-active.spec.ts › works nav item stays non-active on /admin/dashboard
   - result: failed, duration: 5866ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('link', { name: /works/i }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('link', { name: /works/i }) 23 | 24 | const worksLink = page.getByRole('link', { name: /works/i }) > 25 | await expect(worksLink).toBeVisible() | ^ 26 | await expect(worksLink).not.toHaveClass(/font-semibold/) 27 | await expect(worksLink).not.toHaveAttribute('aria-curre
157. [chromium-authenticated] tests/ui-admin-sidebar-links.spec.ts:3 ui-admin-sidebar-links.spec.ts › sidebar exposes a single View Site link
   - result: failed, duration: 5902ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveCount(expected) failed Locator: locator('aside a').filter({ hasText: /View Site|Public Home|Open Site/i }) Expected: 1 Received: 0 Timeout: 5000ms Call log: - Expect "toHaveCount" with timeout 5000ms - waiting for locator('aside a').filter({ hasText: /View Site|Public Home|Open Site/i }) 9 × locator resolved to 0 elements - unexpected value "0" 5 | 6 | const siteLinks = page.locator('aside a').filter({ hasText: /View Site|Public Home|Open Site/i }) > 7 | await expect
158. [chromium-authenticated] tests/ui-admin-sidebar-links.spec.ts:11 ui-admin-sidebar-links.spec.ts › View Site opens in a new tab
   - result: failed, duration: 5831ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveAttribute(expected) failed Locator: getByRole('link', { name: /view site/i }) Expected: "_blank" Timeout: 5000ms Error: element(s) not found Call log: - Expect "toHaveAttribute" with timeout 5000ms - waiting for getByRole('link', { name: /view site/i }) 13 | 14 | const viewSiteLink = page.getByRole('link', { name: /view site/i }) > 15 | await expect(viewSiteLink).toHaveAttribute('target', '_blank') | ^ 16 | }) 17 | at /work/tests/ui-admin-sidebar-links.spec.ts:15:30
159. [chromium-authenticated] tests/ui-admin-sidebar-text.spec.ts:3 ui-admin-sidebar-text.spec.ts › sidebar removes the legacy marketing helper copy
   - result: failed, duration: 5817ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: /admin panel/i }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: /admin panel/i }) 5 | 6 | await expect(page.getByText(/Modernized shortcuts/i)).toHaveCount(0) > 7 | await expect(page.getByRole('heading', { name: /admin panel/i })).toBeVisible() | ^ 8 | }) 9 | at /work/tests/ui-admin-sidebar-text.spec.ts:7
160. [chromium-authenticated] tests/ui-admin-sidebar-width.spec.ts:3 ui-admin-sidebar-width.spec.ts › admin sidebar width collapses to 256px on desktop
   - result: failed, duration: 5858ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('aside').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('aside').first() 6 | 7 | const aside = page.locator('aside').first() > 8 | await expect(aside).toBeVisible() | ^ 9 | 10 | const width = await aside.evaluate((element) => Math.round(element.getBoundingClientRect().width)) 11 | expect(width).toBe(256) at /work/tests/ui-admin-sidebar-wid
161. [chromium-authenticated] tests/ui-admin-sidebar-width.spec.ts:14 ui-admin-sidebar-width.spec.ts › admin main content keeps at least 960px on 1280px viewport
   - result: failed, duration: 5955ms
   - category: admin/editor/authenticated UI
   - symptom: TOC/layout expectation mismatch
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('main').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('main').first() 17 | 18 | const main = page.locator('main').first() > 19 | await expect(main).toBeVisible() | ^ 20 | 21 | const width = await main.evaluate((element) => Math.round(element.getBoundingClientRect().width)) 22 | expect(width).toBeGreaterThanOrEqual(960) at /work/tests/ui-a
162. [chromium-authenticated] tests/ui-admin-table-lang.spec.ts:20 ui-admin-table-lang.spec.ts › admin blog table chrome uses English labels
   - result: failed, duration: 5833ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveAttribute(expected) failed Locator: getByLabel('Search blog titles') Expected pattern: /Search by/i Timeout: 5000ms Error: element(s) not found Call log: - Expect "toHaveAttribute" with timeout 5000ms - waiting for getByLabel('Search blog titles') 6 | await page.goto(path) 7 | > 8 | await expect(page.getByLabel(searchLabel)).toHaveAttribute('placeholder', /Search by/i) | ^ 9 | await expect(page.getByRole('button', { name: 'Previous page' })).toBeVisible() 10 | await 
163. [chromium-authenticated] tests/ui-admin-table-lang.spec.ts:24 ui-admin-table-lang.spec.ts › admin works table chrome uses English labels
   - result: failed, duration: 5976ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toHaveAttribute(expected) failed Locator: getByLabel('Search work titles') Expected pattern: /Search by/i Timeout: 5000ms Error: element(s) not found Call log: - Expect "toHaveAttribute" with timeout 5000ms - waiting for getByLabel('Search work titles') 6 | await page.goto(path) 7 | > 8 | await expect(page.getByLabel(searchLabel)).toHaveAttribute('placeholder', /Search by/i) | ^ 9 | await expect(page.getByRole('button', { name: 'Previous page' })).toBeVisible() 10 | await 
164. [chromium-authenticated] tests/ui-admin-table-polish.spec.ts:59 ui-admin-table-polish.spec.ts › admin blog list renders short tag lists as separate badges
   - result: timedOut, duration: 32227ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 15 | async function createBlog(page: Page, title: string, tags: string) { 16 | await page.goto('/admin/blog/new') > 17 | await page.getByLabel('Title').fill(title) | ^ 18 | await page.getByLabel('Tags (comma separated)').fill(tags) 19 | await fillEditor(page, `Regression coverage body for ${title}`) 20 | at createBlog (/work/tests/ui-admin-table-polish.spec.ts:17:3
165. [chromium-authenticated] tests/ui-admin-table-polish.spec.ts:76 ui-admin-table-polish.spec.ts › admin blog list collapses extra tags into an overflow badge
   - result: timedOut, duration: 32085ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 15 | async function createBlog(page: Page, title: string, tags: string) { 16 | await page.goto('/admin/blog/new') > 17 | await page.getByLabel('Title').fill(title) | ^ 18 | await page.getByLabel('Tags (comma separated)').fill(tags) 19 | await fillEditor(page, `Regression coverage body for ${title}`) 20 | at createBlog (/work/tests/ui-admin-table-polish.spec.ts:17:3
166. [chromium-authenticated] tests/ui-admin-table-polish.spec.ts:96 ui-admin-table-polish.spec.ts › admin works list includes a thumbnail column
   - result: timedOut, duration: 32022ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 27 | async function createWork(page: Page, title: string) { 28 | await page.goto('/admin/works/new') > 29 | await page.getByLabel('Title').fill(title) | ^ 30 | await page.getByLabel('Category').fill('regression') 31 | await fillEditor(page, `Regression coverage body for ${title}`) 32 | at createWork (/work/tests/ui-admin-table-polish.spec.ts:29:34) at /work/tests/u
167. [chromium-authenticated] tests/ui-admin-table-polish.spec.ts:112 ui-admin-table-polish.spec.ts › admin blog list uses compact pagination controls
   - result: failed, duration: 5846ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeDisabled() failed Locator: getByRole('button', { name: 'Previous page' }) Expected: disabled Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeDisabled" with timeout 5000ms - waiting for getByRole('button', { name: 'Previous page' }) 50 | 51 | async function expectCompactPagination(page: Page) { > 52 | await expect(page.getByRole('button', { name: 'Previous page' })).toBeDisabled() | ^ 53 | await expect(page.getByRole('button', { name: 'Next page' }))
168. [chromium-authenticated] tests/ui-admin-table-polish.spec.ts:117 ui-admin-table-polish.spec.ts › admin works list uses compact pagination controls
   - result: failed, duration: 5829ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeDisabled() failed Locator: getByRole('button', { name: 'Previous page' }) Expected: disabled Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeDisabled" with timeout 5000ms - waiting for getByRole('button', { name: 'Previous page' }) 50 | 51 | async function expectCompactPagination(page: Page) { > 52 | await expect(page.getByRole('button', { name: 'Previous page' })).toBeDisabled() | ^ 53 | await expect(page.getByRole('button', { name: 'Next page' }))
169. [chromium-authenticated] tests/ui-admin-table-selection-visual.spec.ts:5 ui-admin-table-selection-visual.spec.ts › VA-213 selected admin table rows gain a visual selected state
   - result: failed, duration: 5709ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Error: expect(locator).toBeVisible() failed Locator: getByTestId('admin-blog-row').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByTestId('admin-blog-row').first() 7 | 8 | const row = page.getByTestId('admin-blog-row').first() > 9 | await expect(row).toBeVisible() | ^ 10 | await row.getByRole('checkbox').check() 11 | 12 | await expect.poll(() => row.getAttribute('data-state')).toBe('selected') at /work/
170. [chromium-authenticated] tests/ui-admin-tiptap-link-popover.spec.ts:23 ui-admin-tiptap-link-popover.spec.ts › link toolbar opens a popover and applies links without browser dialogs
   - result: timedOut, duration: 31998ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByRole('link').first() 8 | async function openBlogEditor(page: Page) { 9 | await page.goto('/admin/blog') > 10 | await page.getByTestId('admin-blog-row').first().getByRole('link').first().click() | ^ 11 | await expect(page.locator('.tiptap.ProseMirror').first()).toBeVisible() 12 | await expect.poll(() => page.evaluate(() => { 13 | const target
171. [chromium-authenticated] tests/ui-admin-tiptap-link-popover.spec.ts:67 ui-admin-tiptap-link-popover.spec.ts › link toolbar removes existing links from the current selection
   - result: timedOut, duration: 32010ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByRole('link').first() 8 | async function openBlogEditor(page: Page) { 9 | await page.goto('/admin/blog') > 10 | await page.getByTestId('admin-blog-row').first().getByRole('link').first().click() | ^ 11 | await expect(page.locator('.tiptap.ProseMirror').first()).toBeVisible() 12 | await expect.poll(() => page.evaluate(() => { 13 | const target
172. [chromium-authenticated] tests/ui-admin-tiptap-semantic.spec.ts:13 ui-admin-tiptap-semantic.spec.ts › dark mode tiptap surfaces resolve to semantic background tokens
   - result: timedOut, duration: 31950ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByRole('link').first() 7 | async function openDarkBlogEditor(page: Page) { 8 | await gotoWithTheme(page, '/admin/blog') > 9 | await page.getByTestId('admin-blog-row').first().getByRole('link').first().click() | ^ 10 | await expect(page.getByTestId('tiptap-editor-shell')).toBeVisible() 11 | } 12 | at openDarkBlogEditor (/work/tests/ui-admin-tip
173. [chromium-authenticated] tests/ui-admin-tiptap-sticky-toolbar.spec.ts:20 ui-admin-tiptap-sticky-toolbar.spec.ts › editor toolbar stays pinned while scrolling long content
   - result: failed, duration: 5883ms
   - category: admin/editor/authenticated UI
   - symptom: redirected to login
   - error: Error: expect(locator).toHaveAttribute(expected) failed Locator: getByTestId('admin-blog-row').first().getByRole('link').first() Expected pattern: /\/admin\/blog\/[^?]+/ Timeout: 5000ms Error: element(s) not found Call log: - Expect "toHaveAttribute" with timeout 5000ms - waiting for getByTestId('admin-blog-row').first().getByRole('link').first() 7 | await page.goto('/admin/blog') 8 | const editLink = page.getByTestId('admin-blog-row').first().getByRole('link').first() > 9 | await expect(editLin
174. [chromium-authenticated] tests/ui-admin-unsaved-dialog.spec.ts:5 ui-admin-unsaved-dialog.spec.ts › VA-233 blog editor shows a warning dialog with keep and discard actions when cancelling dirty changes
   - result: timedOut, duration: 31976ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 5 | test('VA-233 blog editor shows a warning dialog with keep and discard actions when cancelling dirty changes', async ({ page }) => { 6 | await page.goto('/admin/blog/new') > 7 | await page.getByLabel('Title').fill(`Unsaved Blog ${Date.now()}`) | ^ 8 | await page.getByRole('button', { name: 'Cancel' }).click() 9 | 10 | const dialog = page.getByTestId('admin-unsav
175. [chromium-authenticated] tests/ui-admin-unsaved-dialog.spec.ts:17 ui-admin-unsaved-dialog.spec.ts › VA-233 work editor shows a warning dialog with keep and discard actions when cancelling dirty changes
   - result: timedOut, duration: 32014ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 17 | test('VA-233 work editor shows a warning dialog with keep and discard actions when cancelling dirty changes', async ({ page }) => { 18 | await page.goto('/admin/works/new') > 19 | await page.getByLabel('Title').fill(`Unsaved Work ${Date.now()}`) | ^ 20 | await page.getByRole('button', { name: 'Cancel' }).click() 21 | 22 | const dialog = page.getByTestId('admin
176. [chromium-authenticated] tests/ui-admin-unsaved-warning.spec.ts:5 ui-admin-unsaved-warning.spec.ts › blog editor registers beforeunload only after a real change
   - result: timedOut, duration: 32166ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('object') 9 | > 10 | await page.getByLabel('Title').fill(`Dirty blog ${Date.now()}`) | ^ 11 | await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('function') 12 | }) 13 | at /work/tests/ui-admin-unsaved-warning.spec.ts:10:34
177. [chromium-authenticated] tests/ui-admin-unsaved-warning.spec.ts:14 ui-admin-unsaved-warning.spec.ts › work editor registers beforeunload only after a real change
   - result: timedOut, duration: 32190ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 17 | await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('object') 18 | > 19 | await page.getByLabel('Title').fill(`Dirty work ${Date.now()}`) | ^ 20 | await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('function') 21 | }) 22 | at /work/tests/ui-admin-unsaved-warning.spec.ts:19:34
178. [chromium-authenticated] tests/ui-admin-unsaved-warning.spec.ts:23 ui-admin-unsaved-warning.spec.ts › blog editor clears beforeunload after a successful save
   - result: timedOut, duration: 32198ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.getAttribute: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-blog-row').first().getByRole('link').first() 25 | 26 | await page.goto('/admin/blog') > 27 | const editHref = await page.getByTestId('admin-blog-row').first().getByRole('link').first().getAttribute('href') | ^ 28 | if (!editHref) { 29 | throw new Error('Missing blog edit href') 30 | } at /work/tests/ui-admin-unsaved-warning.spec.ts:27:95
179. [chromium-authenticated] tests/ui-admin-unsaved-warning.spec.ts:46 ui-admin-unsaved-warning.spec.ts › work editor clears beforeunload after a successful save
   - result: timedOut, duration: 32036ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.getAttribute: Test timeout of 30000ms exceeded. Call log: - waiting for getByTestId('admin-work-row').first().getByRole('link', { name: /Edit/i }) 48 | 49 | await page.goto('/admin/works') > 50 | const editHref = await page.getByTestId('admin-work-row').first().getByRole('link', { name: /Edit/i }).getAttribute('href') | ^ 51 | if (!editHref) { 52 | throw new Error('Missing work edit href') 53 | } at /work/tests/ui-admin-unsaved-warning.spec.ts:50:
180. [chromium-authenticated] tests/ui-admin-work-create-cta.spec.ts:5 ui-admin-work-create-cta.spec.ts › work create form shows a single default create CTA before videos are staged
   - result: failed, duration: 5921ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: 'Create Work' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: 'Create Work' }) 6 | await page.goto('/admin/works/new') 7 | > 8 | await expect(page.getByRole('button', { name: 'Create Work' })).toBeVisible() | ^ 9 | await expect(page.getByRole('button', { name: 'Create And Add Videos' })).toHaveCount(0) 10 
181. [chromium-authenticated] tests/ui-admin-work-create-cta.spec.ts:12 ui-admin-work-create-cta.spec.ts › work create CTA switches to the staged-video create label
   - result: timedOut, duration: 32138ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 13 | await page.goto('/admin/works/new') 14 | > 15 | await page.getByLabel('Title').fill(`CTA Singular ${Date.now()}`) | ^ 16 | await page.getByLabel('YouTube URL or ID').fill('https://youtu.be/dQw4w9WgXcQ') 17 | await page.getByRole('button', { name: 'Add YouTube Video' }).click() 18 | at /work/tests/ui-admin-work-create-cta.spec.ts:15:34
182. [chromium-authenticated] tests/ui-admin-work-create-cta.spec.ts:24 ui-admin-work-create-cta.spec.ts › work create CTA stays consolidated when multiple videos are staged
   - result: timedOut, duration: 32111ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 25 | await page.goto('/admin/works/new') 26 | > 27 | await page.getByLabel('Title').fill(`CTA Plural ${Date.now()}`) | ^ 28 | await page.getByLabel('YouTube URL or ID').fill('https://youtu.be/dQw4w9WgXcQ') 29 | await page.getByRole('button', { name: 'Add YouTube Video' }).click() 30 | await page.getByLabel('YouTube URL or ID').fill('https://youtu.be/oHg5SJYRHA0') a
183. [chromium-authenticated] tests/ui-admin-work-editor-tabs.spec.ts:5 ui-admin-work-editor-tabs.spec.ts › WorkEditor exposes General, Media & Videos, and Content tabs
   - result: failed, duration: 5875ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('tab', { name: 'General' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('tab', { name: 'General' }) 6 | await page.goto('/admin/works/new') 7 | > 8 | await expect(page.getByRole('tab', { name: 'General' })).toBeVisible() | ^ 9 | await expect(page.getByRole('tab', { name: 'Media & Videos' })).toBeVisible() 10 | await expect(page.getByRole('t
184. [chromium-authenticated] tests/ui-admin-work-editor-tabs.spec.ts:13 ui-admin-work-editor-tabs.spec.ts › tab navigation reveals the expected work editor sections and preserves entered data
   - result: timedOut, duration: 32272ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 15 | 16 | await page.goto('/admin/works/new') > 17 | await page.getByLabel('Title').fill(title) | ^ 18 | 19 | await page.getByRole('tab', { name: 'Media & Videos' }).click() 20 | await expect(page.locator('#work-thumbnail-upload')).toBeVisible() at /work/tests/ui-admin-work-editor-tabs.spec.ts:17:34
185. [chromium-authenticated] tests/ui-admin-work-metadata-ui.spec.ts:5 ui-admin-work-metadata-ui.spec.ts › structured metadata fields can be added and removed without raw JSON editing
   - result: timedOut, duration: 32120ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('tab', { name: 'Media & Videos' }) 5 | test('structured metadata fields can be added and removed without raw JSON editing', async ({ page }) => { 6 | await page.goto('/admin/works/new') > 7 | await page.getByRole('tab', { name: 'Media & Videos' }).click() | ^ 8 | 9 | await page.getByRole('button', { name: 'Add Field' }).click() 10 | const keyInput = page.locator('input[id^=
186. [chromium-authenticated] tests/ui-admin-work-metadata-ui.spec.ts:23 ui-admin-work-metadata-ui.spec.ts › Media tab no longer renders a raw JSON textarea for flexible metadata
   - result: timedOut, duration: 32141ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('tab', { name: 'Media & Videos' }) 23 | test('Media tab no longer renders a raw JSON textarea for flexible metadata', async ({ page }) => { 24 | await page.goto('/admin/works/new') > 25 | await page.getByRole('tab', { name: 'Media & Videos' }).click() | ^ 26 | 27 | await expect(page.locator('#work-editor-media-section textarea')).toHaveCount(0) 28 | }) at /work/tests/ui-adm
187. [chromium-authenticated] tests/ui-loading-states.spec.ts:20 ui-loading-states.spec.ts › WQ-024 admin dashboard transitions expose loading chrome before the dashboard resolves
   - result: timedOut, duration: 32522ms
   - category: admin/editor/authenticated UI
   - symptom: 33s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.evaluate: Test timeout of 30000ms exceeded. Call log: - waiting for locator('aside nav').getByRole('link', { name: 'Dashboard' }) 22 | 23 | const dashboardLink = page.locator('aside nav').getByRole('link', { name: 'Dashboard' }) > 24 | await dashboardLink.evaluate((element) => { | ^ 25 | ;(element as HTMLAnchorElement).href = '/admin/dashboard?__qaSlow=1' 26 | }) 27 | await dashboardLink.click() at /work/tests/ui-loading-states.spec.ts:24:23
188. [chromium-authenticated] tests/ui-quality-blog-detail-visuals.spec.ts:7 ui-quality-blog-detail-visuals.spec.ts › VA-120 blog TOC stays visually separated from the article body
   - result: failed, duration: 1223ms
   - category: public content/layout expectation
   - symptom: TOC/layout expectation mismatch
   - error: Error: expect(received).toBe(expected) // Object.is equality Expected: "sticky" Received: "static" 23 | expect(px(styles.borderTopWidth)).toBeGreaterThan(0) 24 | expect(styles.backgroundColor).not.toBe('rgba(0, 0, 0, 0)') > 25 | expect(styles.position).toBe('sticky') | ^ 26 | }) 27 | 28 | test('VA-121 blog body keeps readable paragraph line-height and spacing', async ({ page }) => { at /work/tests/ui-quality-blog-detail-visuals.spec.ts:25:27
189. [chromium-authenticated] tests/ui-quality-layout-shift.spec.ts:35 ui-quality-layout-shift.spec.ts › WQ-023 admin slow dashboard transition keeps cumulative layout shift below 0.1
   - result: timedOut, duration: 32150ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.evaluate: Test timeout of 30000ms exceeded. Call log: - waiting for locator('aside nav').getByRole('link', { name: 'Dashboard' }) 38 | 39 | const dashboardLink = page.locator('aside nav').getByRole('link', { name: 'Dashboard' }) > 40 | await dashboardLink.evaluate((element) => { | ^ 41 | ;(element as HTMLAnchorElement).href = '/admin/dashboard?__qaSlow=1' 42 | }) 43 | await dashboardLink.click() at /work/tests/ui-quality-layout-shift.spec.ts:40:23
190. [chromium-authenticated] tests/ui-quality-responsive-admin.spec.ts:24 ui-quality-responsive-admin.spec.ts › WQ-033 admin sidebar collapses into a stacked top rail on mobile without horizontal overflow
   - result: failed, duration: 5707ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('aside').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('aside').first() 28 | const aside = page.locator('aside').first() 29 | const main = page.locator('main').first() > 30 | await expect(aside).toBeVisible() | ^ 31 | await expect(main).toBeVisible() 32 | 33 | const [asideBox, mainBox, overflowX] = await Promise.all([ at /work/tests/ui-qu
191. [chromium-authenticated] tests/ui-quality-spacing-system.spec.ts:9 ui-quality-spacing-system.spec.ts › VA-020 key layout paddings and gaps follow the 4px spacing rhythm
   - result: timedOut, duration: 31977ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.evaluate: Test timeout of 30000ms exceeded. Call log: - waiting for locator('main').first() 20 | 21 | const adminSamples = await Promise.all([ > 22 | page.locator('main').first().evaluate((element: HTMLElement) => ({ name: 'admin main padding top', value: getComputedStyle(element).paddingTop })), | ^ 23 | page.locator('aside').first().evaluate((element: HTMLElement) => ({ name: 'admin aside padding left', value: getComputedStyle(element).paddingLe
192. [chromium-authenticated] tests/ui-quality-toast-accessibility.spec.ts:6 ui-quality-toast-accessibility.spec.ts › WQ-012 admin toasts announce politely without stealing focus
   - result: timedOut, duration: 32075ms
   - category: admin/editor/authenticated UI
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 19 | 20 | await page.goto('/admin/blog/new') > 21 | await page.getByLabel('Title').fill(`Toast A11y ${Date.now()}`) | ^ 22 | await page.locator('form .tiptap.ProseMirror').first().click() 23 | await page.keyboard.type('Trigger a toast while keeping focus stable.') 24 | at /work/tests/ui-quality-toast-accessibility.spec.ts:21:34
193. [chromium-authenticated] tests/ui-quality-token-consistency.spec.ts:6 ui-quality-token-consistency.spec.ts › VA-001 primary and destructive surfaces resolve from the shared semantic tokens
   - result: failed, duration: 5828ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('button', { name: 'Create Post' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('button', { name: 'Create Post' }) 8 | 9 | const saveButton = page.getByRole('button', { name: 'Create Post' }) > 10 | await expect(saveButton).toBeVisible() | ^ 11 | const primaryBg = await getColorChannels(saveButton, 'background-color') 12 | const expectedPrim
194. [chromium-authenticated] tests/ui-quality-visual-advanced.spec.ts:50 ui-quality-visual-advanced.spec.ts › VA-120 blog detail table of contents keeps its own surfaced container styling
   - result: failed, duration: 915ms
   - category: other
   - symptom: TOC/layout expectation mismatch
   - error: Error: expect(received).toBe(expected) // Object.is equality Expected: "sticky" Received: "static" 65 | expect(backgroundColor).not.toBe('rgba(0, 0, 0, 0)') 66 | expect(boxShadow).not.toBe('none') > 67 | expect(position).toBe('sticky') | ^ 68 | }) 69 | 70 | test('VA-142 work detail metadata stays visually subordinate to the page title', async ({ page }) => { at /work/tests/ui-quality-visual-advanced.spec.ts:67:20
195. [chromium-authenticated] tests/ui-quality-visual-contracts.spec.ts:56 ui-quality-visual-contracts.spec.ts › VA-202 admin sidebar text keeps readable heading and nav label sizing
   - result: failed, duration: 5686ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('heading', { name: 'Admin Panel' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('heading', { name: 'Admin Panel' }) 59 | const heading = page.getByRole('heading', { name: 'Admin Panel' }) 60 | const navLink = page.locator('aside nav').getByRole('link', { name: 'Dashboard' }) > 61 | await expect(heading).toBeVisible() | ^ 62 | await expect(n
196. [chromium-authenticated] tests/ui-quality-visual-contracts.spec.ts:73 ui-quality-visual-contracts.spec.ts › VA-211 and VA-212 admin table columns and search field keep aligned readable controls
   - result: failed, duration: 5822ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Error: expect(locator).toBeVisible() failed Locator: getByRole('textbox', { name: 'Search blog titles' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for getByRole('textbox', { name: 'Search blog titles' }) 77 | const row = page.getByTestId('admin-blog-row').first() 78 | const cells = row.getByRole('cell') > 79 | await expect(search).toBeVisible() | ^ 80 | await expect(row).toBeVisible() 81 | 82 | await expect.poll(
197. [chromium-authenticated] tests/ui-quality-visual-metrics.spec.ts:85 ui-quality-visual-metrics.spec.ts › VA-202, VA-210, VA-211, and VA-212 admin navigation and tables keep readable alignment and touch-friendly rows
   - result: failed, duration: 5796ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('aside nav').first().getByRole('link', { name: 'Dashboard' }) Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('aside nav').first().getByRole('link', { name: 'Dashboard' }) 88 | const nav = page.locator('aside nav').first() 89 | const navButton = nav.getByRole('link', { name: 'Dashboard' }) > 90 | await expect(navButton).toBeVisible() | ^ 91 | const
198. [chromium-authenticated] tests/ui-quality-visual-metrics.spec.ts:107 ui-quality-visual-metrics.spec.ts › VA-221 and VA-223 admin editors keep distinct field surfaces and a prominent save button
   - result: failed, duration: 5811ms
   - category: admin/editor/authenticated UI
   - symptom: 6s timeout
   - error: Error: expect(locator).toBeVisible() failed Locator: locator('.space-y-4.rounded-2xl.border').first() Expected: visible Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeVisible" with timeout 5000ms - waiting for locator('.space-y-4.rounded-2xl.border').first() 110 | const editorShell = page.locator('.space-y-4.rounded-2xl.border').first() 111 | const saveButton = page.getByRole('button', { name: /Create Post|Update Post/i }) > 112 | await expect(editorShell).toBeVisible() | ^ 
199. [chromium-authenticated] tests/ui-quality-visual-metrics.spec.ts:124 ui-quality-visual-metrics.spec.ts › VA-231, VA-232, and VA-404 admin delete dialog uses overlay, elevation, and entry animation tokens
   - result: timedOut, duration: 32042ms
   - category: admin/editor/authenticated UI
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: 'Delete' }).first() 124 | test('VA-231, VA-232, and VA-404 admin delete dialog uses overlay, elevation, and entry animation tokens', async ({ page }) => { 125 | await page.goto('/admin/blog') > 126 | await page.getByRole('button', { name: 'Delete' }).first().click() | ^ 127 | 128 | const overlay = page.locator('[data-slot="dialog-overlay"]').first() 129 | 
200. [chromium-authenticated] tests/ui-quality-z-index.spec.ts:21 ui-quality-z-index.spec.ts › VA-033 dialogs render above dropdown layering
   - result: failed, duration: 5814ms
   - category: admin/editor/authenticated UI
   - symptom: admin row not reachable
   - error: Error: expect(locator).toBeEnabled() failed Locator: getByTestId('admin-blog-row').first().getByRole('button', { name: /^Delete post:/ }) Expected: enabled Timeout: 5000ms Error: element(s) not found Call log: - Expect "toBeEnabled" with timeout 5000ms - waiting for getByTestId('admin-blog-row').first().getByRole('button', { name: /^Delete post:/ }) 22 | await page.goto('/admin/blog') 23 | const deleteButton = page.getByTestId('admin-blog-row').first().getByRole('button', { name: /^Delete post:/
201. [chromium-authenticated] tests/work-green-video-thumbnail.spec.ts:6 work-green-video-thumbnail.spec.ts › uploaded green-only mp4 generates and persists an automatic thumbnail
   - result: timedOut, duration: 32116ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 8 | 9 | await page.goto('/admin/works/new') > 10 | await page.getByLabel('Title').fill(title) | ^ 11 | await page.getByLabel('Category').fill('video') 12 | await page.locator('.tiptap.ProseMirror').first().fill('Green-only video thumbnail verification') 13 | at /work/tests/work-green-video-thumbnail.spec.ts:10:34
202. [chromium-authenticated] tests/work-green-video-thumbnail.spec.ts:31 work-green-video-thumbnail.spec.ts › uploaded green-only mp4 thumbnail is green at the pixel level
   - result: timedOut, duration: 32069ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 33 | 34 | await page.goto('/admin/works/new') > 35 | await page.getByLabel('Title').fill(title) | ^ 36 | await page.getByLabel('Category').fill('video') 37 | await page.locator('.tiptap.ProseMirror').first().fill('Green-only pixel verification') 38 | at /work/tests/work-green-video-thumbnail.spec.ts:35:34
203. [chromium-authenticated] tests/work-inline-create-flow.spec.ts:9 work-inline-create-flow.spec.ts › public works inline create saves in place and closes after a text-only create
   - result: timedOut, duration: 31789ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '새 작업 쓰기' }) 11 | 12 | await page.goto('/works') > 13 | await page.getByRole('button', { name: '새 작업 쓰기' }).click() | ^ 14 | await page.getByLabel('Title').fill(title) 15 | await page.getByLabel('Category').fill('inline') 16 | await page.locator('.tiptap.ProseMirror').first().fill('Public inline create body') at /work/tests/work-inline-create-flow.spec.ts:
204. [chromium-authenticated] tests/work-inline-create-flow.spec.ts:31 work-inline-create-flow.spec.ts › public works inline create with staged videos stays on /works and refreshes the list
   - result: timedOut, duration: 31654ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '새 작업 쓰기' }) 33 | 34 | await page.goto('/works') > 35 | await page.getByRole('button', { name: '새 작업 쓰기' }).click() | ^ 36 | await page.getByLabel('Title').fill(title) 37 | await page.getByLabel('Category').fill('video') 38 | await page.locator('.tiptap.ProseMirror').first().fill('Public inline create video body') at /work/tests/work-inline-create-flow.spe
205. [chromium-authenticated] tests/work-inline-create-flow.spec.ts:58 work-inline-create-flow.spec.ts › existing work video uploads persist thumbnails immediately without requiring Update Work
   - result: timedOut, duration: 32063ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 60 | 61 | await page.goto('/admin/works/new') > 62 | await page.getByLabel('Title').fill(title) | ^ 63 | await page.getByLabel('Category').fill('video') 64 | await page.locator('.tiptap.ProseMirror').first().fill('Thumbnail persistence body') 65 | at /work/tests/work-inline-create-flow.spec.ts:62:34
206. [chromium-authenticated] tests/work-inline-redirects.spec.ts:9 work-inline-redirects.spec.ts › public works inline create returns to the first works page after save
   - result: timedOut, duration: 31690ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '새 작업 쓰기' }) 12 | 13 | await page.goto(`/works?page=2&pageSize=${expectedPageSize}`) > 14 | await page.getByRole('button', { name: '새 작업 쓰기' }).click() | ^ 15 | await page.getByLabel('Title').fill(title) 16 | await page.getByLabel('Category').fill('redirect') 17 | await page.locator('.tiptap.ProseMirror').first().fill('Redirect create body') at /work/tests
207. [chromium-authenticated] tests/work-inline-redirects.spec.ts:31 work-inline-redirects.spec.ts › public work detail inline edit returns the user to the originating works page after save
   - result: timedOut, duration: 31552ms
   - category: works video/admin media
   - symptom: 32s timeout
   - error: Test timeout of 30000ms exceeded. Error: locator.click: Test timeout of 30000ms exceeded. Call log: - waiting for getByRole('button', { name: '작업 수정' }) 37 | await page.locator('a[href^="/works/"]').first().click() 38 | > 39 | await page.getByRole('button', { name: '작업 수정' }).click() | ^ 40 | await page.locator('input#title').fill(updatedTitle) 41 | 42 | await Promise.all([ at /work/tests/work-inline-redirects.spec.ts:39:53
208. [chromium-authenticated] tests/work-inline-redirects.spec.ts:53 work-inline-redirects.spec.ts › public work detail video-only edits enable Update Work without requiring body changes
   - result: timedOut, duration: 32164ms
   - category: works video/admin media
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 55 | 56 | await page.goto('/admin/works/new') > 57 | await page.getByLabel('Title').fill(title) | ^ 58 | await page.getByLabel('Category').fill('video') 59 | await page.locator('.tiptap.ProseMirror').first().fill('video only refresh body') 60 | at /work/tests/work-inline-redirects.spec.ts:57:34
209. [chromium-authenticated] tests/work-single-delete-ux.spec.ts:5 work-single-delete-ux.spec.ts › single work delete uses the in-app dialog and removes the row
   - result: timedOut, duration: 32340ms
   - category: public content/layout expectation
   - symptom: editor title field not reachable
   - error: Test timeout of 30000ms exceeded. Error: locator.fill: Test timeout of 30000ms exceeded. Call log: - waiting for getByLabel('Title') 7 | 8 | await page.goto('/admin/works/new') > 9 | await page.getByLabel('Title').fill(title) | ^ 10 | await page.getByLabel('Category').fill('qa') 11 | await page.locator('.tiptap.ProseMirror').first().fill('delete me') 12 | at /work/tests/work-single-delete-ux.spec.ts:9:34
