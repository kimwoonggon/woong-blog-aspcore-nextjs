import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('work editor blocks invalid metadata json before submit', async ({ page }) => {
  await page.goto('/admin/works/new')
  await expect(page).toHaveURL(/\/admin\/works\/new/)

  await page.getByLabel('Title').fill('Invalid JSON Work')
  await page.getByLabel('Category').fill('validation')
  await page.getByLabel('Flexible Metadata (JSON)').fill('{not-valid-json')
  await page.getByRole('button', { name: 'Create Work' }).click()

  await expect(page.getByText('Invalid JSON in Flexible Metadata field')).toBeVisible()
  await expect(page).toHaveURL(/\/admin\/works\/new/)
})

test('work editor shows save failure for missing required fields', async ({ page }) => {
  await page.goto('/admin/works/new')
  await expect(page).toHaveURL(/\/admin\/works\/new/)

  await expect(page.getByRole('button', { name: 'Create Work' })).toBeDisabled()
  await expect(page).toHaveURL(/\/admin\/works\/new/)
})
