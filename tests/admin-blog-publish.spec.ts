import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can create and publish a blog post that appears on public blog page', async ({ page }) => {
  const title = `Playwright Post ${Date.now()}`

  await page.goto('/admin/blog/new')

  await expect(page).toHaveURL(/\/admin\/blog\/new/)
  await expect(page.getByText('New posts publish immediately when you save.')).toBeVisible()
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Tags (comma separated)').fill('playwright, regression')

  const editor = page.locator('.tiptap.ProseMirror').first()
  await editor.click()
  await page.keyboard.type(`This is a browser-driven published post for ${title}.`)

  await page.getByRole('button', { name: /Create Post/i }).click()

  await expect(page).toHaveURL(/\/admin\/blog(?:\?.*)?$/)
  await expect(page.getByText(title)).toBeVisible()

  await page.goto('/blog')
  await expect(page.getByRole('link', { name: title })).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/admin-blog-publish.png', fullPage: true })
})
