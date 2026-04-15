import { expect, test, type Page } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

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

test('AF-042 autosave status shows Saving then Saved after notion content changes', async ({ page }) => {
  const autosaveText = `AF-042 autosave success ${Date.now()}`
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

  await openNotionWorkspace(page)
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

test('AF-042 autosave status shows Error when notion autosave fails', async ({ page }) => {
  const autosaveText = `AF-042 autosave failure ${Date.now()}`
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

  await openNotionWorkspace(page)

  await replaceEditorContent(page, autosaveText)

  await expect(page.getByTestId('notion-save-state')).toHaveText('Saving...')
  await expect(page.getByTestId('notion-save-state')).toHaveText('Error')
  expect(failedAutosaveSeen).toBeTruthy()
})

test('AF-045 notion document info panel shows timestamps and slug for the selected post', async ({ page }) => {
  await openNotionWorkspace(page)

  const blogs = await page.evaluate(async () => {
    const response = await fetch('/api/admin/blogs')
    if (!response.ok) {
      throw new Error(`Failed to load blogs: ${response.status}`)
    }

    return response.json() as Promise<Array<{
      id: string
      slug: string
      updatedAt?: string | null
      publishedAt?: string | null
    }>>
  })

  const candidate = blogs.find((blog) => blog.updatedAt && blog.publishedAt) ?? blogs.find((blog) => blog.updatedAt) ?? blogs[0]
  if (!candidate) {
    throw new Error('Expected at least one blog document for notion workspace assertions')
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
