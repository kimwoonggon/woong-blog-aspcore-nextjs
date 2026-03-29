import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can create, observe, and apply a blog AI batch job without blocking the workspace', async ({ page }) => {
  test.setTimeout(180_000)

  await page.goto('/admin/blog')
  await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible()

  const checkboxes = page.getByTestId('admin-blog-row').getByRole('checkbox')
  const count = await checkboxes.count()
  expect(count).toBeGreaterThan(1)

  await checkboxes.nth(0).click()
  await checkboxes.nth(1).click()

  await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeVisible()
  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toHaveCount(0)
  await page.getByRole('button', { name: 'Batch AI Fix' }).click()
  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Generate AI Fix job' })).toBeEnabled()

  await page.getByRole('button', { name: 'Generate AI Fix job' }).click()

  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toBeVisible()
  await expect(page.getByTestId('admin-blog-batch-ai-status')).toContainText(/queued|running|processed/i)

  await expect(page.getByTestId('admin-blog-batch-ai-status')).toContainText('completed', { timeout: 180_000 })
  await expect(page.getByTestId('admin-blog-batch-ai-status')).toContainText(/\d+\/\d+ processed/, { timeout: 180_000 })
  await expect(page.getByRole('button', { name: 'Apply all successful' })).toBeVisible()

  await page.getByRole('button', { name: 'Apply all successful' }).click()

  await expect(page.getByText('applied').first()).toBeVisible({ timeout: 30_000 })
})
