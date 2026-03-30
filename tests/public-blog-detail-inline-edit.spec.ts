import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit a public blog detail inline and save in place', async ({ page }) => {
  const updatedBody = `inline blog body ${Date.now()}`

  await page.goto('/blog?page=2&pageSize=1')
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

  await expect(page).toHaveURL(/\/blog\/.*\?page=2&pageSize=1/)
  await expect(page.getByText(updatedBody).first()).toBeVisible()
  await expect(page.getByTestId('related-blog-card').first()).toHaveAttribute('href', /page=2&pageSize=1/)
})

test('public blog detail shows paginated related posts', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog?pageSize=1')
  await page.locator('a[href^="/blog/"]').first().click()

  await expect(page.getByRole('heading', { name: '다른 게시물' })).toBeVisible()
  await expect(page.getByTestId('related-blog-card').first()).toBeVisible()
  await expect(page.getByTestId('related-blog-pagination-status')).toHaveText(/1\s*\/\s*\d+/)
  await expect(page.getByRole('button', { name: '2' })).toBeVisible()
})
