import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can create, observe, and apply a blog AI batch job without blocking the workspace', async ({ page }) => {
  test.setTimeout(180_000)

  await page.goto('/admin/blog')
  await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible()

  const rows = page.getByTestId('admin-blog-row')
  const count = await rows.count()
  expect(count).toBeGreaterThan(1)

  await rows.nth(0).getByRole('checkbox').click()
  await rows.nth(1).getByRole('checkbox').click()

  await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeVisible()
  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toHaveCount(0)
  await page.getByRole('button', { name: 'Batch AI Fix' }).click()
  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toBeVisible()
  const clearCompleted = page.getByRole('button', { name: /Clear completed/i })
  if (await clearCompleted.isVisible().catch(() => false)) {
    await clearCompleted.click()
    await expect(clearCompleted).toHaveCount(0)
  }
  await expect(page.getByRole('button', { name: 'Generate AI Fix job' })).toBeEnabled()

  await page.getByRole('button', { name: 'Generate AI Fix job' }).click()

  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toBeVisible()
  await expect(page.getByTestId('admin-blog-batch-ai-status')).toContainText(/queued|running|processed/i)

  await expect(page.getByTestId('admin-blog-batch-ai-status')).toContainText('completed', { timeout: 180_000 })
  await expect(page.getByTestId('admin-blog-batch-ai-status')).toContainText('2/2 processed', { timeout: 180_000 })
  const applyAll = page.getByRole('button', { name: 'Apply all successful' })
  await expect(applyAll).toBeVisible({ timeout: 30_000 })
  await applyAll.click()

  await expect(page.getByText('applied').first()).toBeVisible({ timeout: 30_000 })
})
