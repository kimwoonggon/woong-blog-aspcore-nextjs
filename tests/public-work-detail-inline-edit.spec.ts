import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit a public work detail inline and save in place', async ({ page }) => {
  const updatedBody = `inline work body ${Date.now()}`

  await page.goto('/works?pageSize=1')
  await page.locator('a[href^="/works/"]').first().click()

  await expect(page.getByRole('button', { name: '작업 수정' })).toBeVisible()
  await page.getByRole('button', { name: '작업 수정' }).click()

  const saveButton = page.getByRole('button', { name: 'Update Work' })
  await expect(saveButton).toBeDisabled()

  await page.locator('#content').fill(`<p>${updatedBody}</p>`)
  await expect(saveButton).toBeEnabled()

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/works/') && res.request().method() === 'PUT' && res.ok()),
    saveButton.click(),
  ])

  await expect(page.getByText(updatedBody).first()).toBeVisible()
})

test('public work detail shows paginated related items', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/works?pageSize=1')
  await page.locator('a[href^="/works/"]').first().click()

  await expect(page.getByRole('heading', { name: '다른 작업' })).toBeVisible()
  await expect(page.getByTestId('related-work-card').first()).toBeVisible()
  await expect(page.getByText(/1\s*\/\s*\d+/)).toBeVisible()
  await expect(page.getByRole('button', { name: '2' })).toBeVisible()
})
