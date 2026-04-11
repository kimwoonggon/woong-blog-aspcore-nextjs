import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('single work delete uses a simple confirm dialog and removes the row', async ({ page }) => {
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

  page.once('dialog', async (dialog) => {
    expect(dialog.type()).toBe('confirm')
    expect(dialog.message()).toContain(`Delete "${title}"?`)
    await dialog.accept()
  })

  await Promise.all([
    page.waitForResponse((response) =>
      response.url().includes('/api/admin/works/') && response.request().method() === 'DELETE' && response.ok(),
    ),
    row.getByTitle('Delete').click(),
  ])

  await expect(row).toHaveCount(0)
})
