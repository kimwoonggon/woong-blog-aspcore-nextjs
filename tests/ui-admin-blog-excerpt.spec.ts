import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('BlogEditor exposes an excerpt field with a live character counter', async ({ page }) => {
  const excerpt = 'x'.repeat(50)

  await page.goto('/admin/blog/new')

  await expect(page.getByLabel('Excerpt')).toBeVisible()
  await page.getByLabel('Excerpt').fill(excerpt)
  await expect(page.getByText('50/200')).toBeVisible()
})
