import { expect, test, type Page } from '@playwright/test'
import { createBlogFixture } from './helpers/content-fixtures'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })
test.setTimeout(60_000)

const selectAllShortcut = process.platform === 'darwin' ? 'Meta+A' : 'Control+A'

function formatTimestamp(value?: string | null) {
  if (!value) {
    return '—'
  }

  return new Date(value).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

async function openNotionWorkspace(page: Page, blogId?: string) {
  const target = blogId ? `/admin/blog/notion?id=${encodeURIComponent(blogId)}` : '/admin/blog/notion'
  await page.goto(target)
  await expect(page.getByRole('heading', { name: 'Blog Notion View' }).first()).toBeVisible()
  await expect(page.locator('.tiptap.ProseMirror').first()).toBeVisible()
}

async function replaceEditorContent(page: Page, nextText: string) {
  const editor = page.locator('.tiptap.ProseMirror').first()
  await editor.click()
  await page.keyboard.press(selectAllShortcut)
  await page.keyboard.type(nextText)
}

test('AF-042 autosave status shows Saving then Saved after notion content changes', async ({ page, request }, testInfo) => {
  const autosaveText = `AF-042 autosave success ${Date.now()}`
  const blog = await createBlogFixture(request, testInfo, {
    titlePrefix: 'Notion Autosave Success',
    html: '<p>Notion autosave success fixture.</p>',
  })
  let delayedAutosaveSeen = false

  await page.route('**/api/admin/blogs/*', async (route) => {
    const request = route.request()
    if (request.method() !== 'PUT') {
      await route.continue()
      return
    }

    const body = request.postData() ?? ''
    if (!body.includes(autosaveText)) {
      await route.continue()
      return
    }

    delayedAutosaveSeen = true
    await page.waitForTimeout(900)
    await route.continue()
  })

  await openNotionWorkspace(page, blog.id)
  await expect(page.getByTestId('notion-save-state')).toHaveText('Waiting')

  const saveResponse = page.waitForResponse((response) =>
    response.url().includes('/api/admin/blogs/')
    && response.request().method() === 'PUT'
    && response.ok(),
  )

  await replaceEditorContent(page, autosaveText)

  await expect(page.getByTestId('notion-save-state')).toHaveText('Saving...')
  await saveResponse
  expect(delayedAutosaveSeen).toBeTruthy()
  await expect(page.getByTestId('notion-save-state')).toHaveText('Saved')
})

test('AF-042 autosave status shows Error when notion autosave fails', async ({ page, request }, testInfo) => {
  const autosaveText = `AF-042 autosave failure ${Date.now()}`
  const blog = await createBlogFixture(request, testInfo, {
    titlePrefix: 'Notion Autosave Failure',
    html: '<p>Notion autosave failure fixture.</p>',
  })
  let failedAutosaveSeen = false

  await page.route('**/api/admin/blogs/*', async (route) => {
    const request = route.request()
    if (request.method() !== 'PUT') {
      await route.continue()
      return
    }

    const body = request.postData() ?? ''
    if (!body.includes(autosaveText) || failedAutosaveSeen) {
      await route.continue()
      return
    }

    failedAutosaveSeen = true
    await page.waitForTimeout(900)
    await route.fulfill({
      status: 500,
      contentType: 'text/plain',
      body: 'Autosave failed in test',
    })
  })

  await openNotionWorkspace(page, blog.id)

  await replaceEditorContent(page, autosaveText)

  await expect(page.getByTestId('notion-save-state')).toHaveText('Saving...')
  await expect(page.getByTestId('notion-save-state')).toHaveText('Error')
  expect(failedAutosaveSeen).toBeTruthy()
})

test('AF-045 notion document info panel shows timestamps and slug for the selected post', async ({ page, request }, testInfo) => {
  const fixture = await createBlogFixture(request, testInfo, {
    titlePrefix: 'Notion Info Panel',
    html: '<p>Notion info panel fixture.</p>',
  })

  const blogsResponse = await request.get('/api/admin/blogs')
  expect(blogsResponse.ok()).toBeTruthy()
  const blogs = await blogsResponse.json() as Array<{
    id: string
    slug: string
    updatedAt?: string | null
    publishedAt?: string | null
  }>

  const candidate = blogs.find((blog) => blog.id === fixture.id)
  if (!candidate) {
    throw new Error('Expected the created blog document for notion workspace assertions')
  }

  await openNotionWorkspace(page, candidate.id)

  const infoPanel = page.getByTestId('notion-doc-info')
  await expect(infoPanel).toBeVisible()
  await expect(infoPanel.getByText('Document info')).toBeVisible()
  await expect(infoPanel.getByText('Published')).toBeVisible()
  await expect(infoPanel.getByText('Last updated')).toBeVisible()
  await expect(infoPanel.getByText('Slug')).toBeVisible()

  await expect(infoPanel.locator('dd').filter({ hasText: candidate.slug })).toBeVisible()
  await expect(infoPanel.locator('dd').filter({ hasText: formatTimestamp(candidate.updatedAt) }).first()).toBeVisible()

  if (candidate.publishedAt) {
    await expect(infoPanel.locator('dd').filter({ hasText: formatTimestamp(candidate.publishedAt) }).first()).toBeVisible()
  } else {
    await expect(infoPanel.locator('dd').filter({ hasText: '—' }).first()).toBeVisible()
  }

  await expect(page.getByRole('button', { name: 'Save Post Settings' })).toBeDisabled()
})
