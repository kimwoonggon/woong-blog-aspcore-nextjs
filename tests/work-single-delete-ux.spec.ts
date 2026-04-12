import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('single work delete uses the in-app dialog and removes the row', async ({ page }) => {
  const title = `Single Delete UX ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('qa')
  await page.locator('.tiptap.ProseMirror').first().fill('delete me')

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/works') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create Work' }).click(),
  ])

  await page.goto('/admin/works')
  const row = page.getByTestId('admin-work-row').filter({ hasText: title }).first()
  await expect(row).toBeVisible()

  await row.getByTitle('Delete').click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()

  await Promise.all([
    page.waitForResponse((response) =>
      response.url().includes('/api/admin/works/') && response.request().method() === 'DELETE' && response.ok(),
    ),
    dialog.getByRole('button', { name: 'Delete' }).click(),
  ])

  await expect(row).toHaveCount(0)
})
