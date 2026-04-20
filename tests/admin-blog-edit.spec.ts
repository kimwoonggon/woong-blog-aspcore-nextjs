import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit an existing blog post with mixed special input', async ({ page }) => {
  const updatedTitle = `수정된 블로그! ${Date.now()} ###`
  const updatedBody = `수정 본문 한국어 + English + !!! ${Date.now()}`

  await page.goto('/admin/blog')
  const firstRow = page.getByTestId('admin-blog-row').first()
  await expect(firstRow).toBeVisible()
  const editLink = firstRow.locator('td').nth(1).getByRole('link').first()
  await expect(editLink).toHaveAttribute('href', /\/admin\/blog\/[^?]+/)
  await Promise.all([
    page.waitForURL(/\/admin\/blog\/[^/?]+/),
    editLink.click(),
  ])
  await expect(page.getByLabel('Title')).toBeVisible()

  await page.getByLabel('Title').fill(updatedTitle)
  const publishedCheckbox = page.getByRole('checkbox', { name: 'Published' })
  if (!(await publishedCheckbox.isChecked())) {
    await publishedCheckbox.click()
  }

  await page.locator('.tiptap.ProseMirror').first().click()
  await page.keyboard.press(process.platform === 'darwin' ? 'Meta+A' : 'Control+A')
  await page.keyboard.type(updatedBody)

  const [response] = await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/blogs/') && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Post' }).click(),
  ])

  const payload = await response.json()
  await page.goto(`/blog/${payload.slug}`)
  await expect(page.locator('main h1', { hasText: updatedTitle })).toBeVisible()
  await expect(page.getByText(updatedBody).first()).toBeVisible()
})

test('blog notion view supports list selection and content autosave', async ({ page }) => {
  const autosaveText = `Autosaved from notion view ${Date.now()}`

  await page.goto('/admin/blog')
  await page.getByRole('link', { name: /Blog Notion View|Notion View/ }).filter({ visible: true }).first().click()
  await expect(page).toHaveURL(/\/admin\/blog\/notion/)
  await expect(page.getByRole('heading', { name: 'Blog Notion View' }).first()).toBeVisible()
  await page.getByTestId('notion-library-trigger').click()
  await expect(page.getByTestId('notion-blog-list-item').first()).toBeVisible()
  await expect(page.getByTestId('tiptap-toolbar-hint')).toContainText('Type / for commands')

  const listItems = page.getByTestId('notion-blog-list-item')
  if ((await listItems.count()) > 1) {
    await listItems.nth(1).click()
  } else {
    await listItems.first().click()
  }

  await expect(page).toHaveURL(/\/admin\/blog\/notion\?id=/)

  const editor = page.locator('.tiptap.ProseMirror').first()
  const saveResponse = page.waitForResponse((response) =>
    response.url().includes('/api/admin/blogs/') && response.request().method() === 'PUT' && response.ok(),
  )
  await editor.click()
  await page.keyboard.press(process.platform === 'darwin' ? 'Meta+A' : 'Control+A')
  await page.keyboard.type(autosaveText)

  await saveResponse
  await expect(page.getByTestId('notion-save-state')).toHaveText('Saved')
  await expect(page.getByLabel('Title')).toBeVisible()
  await expect(page.getByRole('checkbox', { name: 'Published' })).toBeVisible()
})
