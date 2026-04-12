import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit a public blog detail inline and save in place', async ({ page }) => {
  const updatedBody = `inline blog body ${Date.now()}`

  await page.goto('/blog?pageSize=1')
  await page.locator('a[href^="/blog/"]').first().click()

  await expect(page.getByRole('button', { name: '글 수정' })).toBeVisible()
  await page.getByRole('button', { name: '글 수정' }).click()

  const saveButton = page.getByRole('button', { name: 'Update Post' })
  await expect(saveButton).toBeDisabled()

  await page.locator('.tiptap.ProseMirror').first().click()
  await page.keyboard.press(process.platform === 'darwin' ? 'Meta+A' : 'Control+A')
  await page.keyboard.type(updatedBody)
  await expect(saveButton).toBeEnabled()

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/blogs/') && res.request().method() === 'PUT' && res.ok()),
    saveButton.click(),
  ])

  await expect(page.getByText(updatedBody).first()).toBeVisible()
})

test('public blog detail shows paginated related posts', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog?pageSize=1')
  await page.locator('a[href^="/blog/"]').first().click()

  await expect(page.getByRole('heading', { name: 'More Posts' })).toBeVisible()
  await expect(page.getByTestId('related-blog-card').first()).toBeVisible()
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Posts' }) }).getByText(/^Page 1 of \d+$/)).toBeVisible()
  await expect(page.getByRole('button', { name: 'Next' })).toBeVisible()
  await expect(page.getByRole('button', { name: '2' })).toBeVisible()
})
