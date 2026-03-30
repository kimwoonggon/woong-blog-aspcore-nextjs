import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin batch AI panel validates date mode and updates selection summary for range mode', async ({ page }) => {
  await page.goto('/admin/blog')
  await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible()

  const checkboxes = page.getByTestId('admin-blog-row').getByRole('checkbox')
  await checkboxes.nth(0).click()
  await checkboxes.nth(1).click()

  await page.getByRole('button', { name: 'Batch AI Fix' }).click()
  await expect(page.getByTestId('admin-blog-batch-ai-panel')).toBeVisible()

  await page.locator('#batch-mode').selectOption('date')
  await page.getByRole('button', { name: 'Generate AI Fix job' }).click()
  await expect(page.getByText('Set a start date or end date before creating a date-range batch job')).toBeVisible()

  await page.locator('#batch-mode').selectOption('range')
  await page.getByLabel('Batch range start').fill('1')
  await page.getByLabel('Batch range count').fill('2')
  await expect(page.getByText('2 selected').first()).toBeVisible()
})
