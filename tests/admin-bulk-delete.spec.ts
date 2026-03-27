import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can bulk delete selected blogs and works with confirmation', async ({ page }) => {
  await page.goto('/admin/blog')
  const blogRows = page.getByTestId('admin-blog-row')
  if (await blogRows.count()) {
    await page.once('dialog', (dialog) => dialog.accept('yes'))
    await blogRows.first().getByRole('checkbox').click()
    await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeVisible()
    const deleteBlogResponse = page.waitForResponse((response) =>
      response.url().includes('/api/admin/blogs/') && response.request().method() === 'DELETE' && response.ok(),
    )
    await page.getByRole('button', { name: 'Delete Selected' }).click()
    await deleteBlogResponse
  }

  await page.goto('/admin/works')
  const workRows = page.getByTestId('admin-work-row')
  if (await workRows.count()) {
    await page.once('dialog', (dialog) => dialog.accept('yes'))
    await workRows.first().getByRole('checkbox').click()
    await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeVisible()
    const deleteWorkResponse = page.waitForResponse((response) =>
      response.url().includes('/api/admin/works/') && response.request().method() === 'DELETE' && response.ok(),
    )
    await page.getByRole('button', { name: 'Delete Selected' }).click()
    await deleteWorkResponse
  }
})
