import { expect, test } from './helpers/performance-test'
import { expectedPublicWorksPageSize } from './helpers/responsive-policy'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit a public work detail inline and return to the originating works page', async ({ page }) => {
  const updatedTitle = `inline work title ${Date.now()}`
  const expectedPageSize = await expectedPublicWorksPageSize(page)

  await page.goto(`/works?page=2&pageSize=${expectedPageSize}`)
  const originalListUrl = page.url()
  await page.locator('a[href^="/works/"]').first().click()

  await expect(page.getByRole('button', { name: '작업 수정' })).toBeVisible()
  await page.getByRole('button', { name: '작업 수정' }).click()

  const saveButton = page.getByRole('button', { name: 'Update Work' })
  await expect(saveButton).toBeDisabled()

  await page.getByLabel('Title').fill(updatedTitle)
  await expect(saveButton).toBeEnabled()

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/works/') && res.request().method() === 'PUT' && res.ok()),
    saveButton.click(),
  ])

  await expect(page).toHaveURL(originalListUrl)
  await expect(page.getByText(updatedTitle)).toBeVisible()
})

test('public work detail shows paginated related items', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/works?pageSize=1')
  await page.locator('a[href^="/works/"]').first().click()

  await expect(page.getByRole('heading', { name: 'More Works' })).toBeVisible()
  await expect(page.getByTestId('related-work-card').first()).toBeVisible()
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Works' }) }).getByText(/^Page 1 of \d+$/)).toBeVisible()
  await expect(page.getByRole('button', { name: 'Next' })).toBeVisible()
  await expect(page.getByRole('button', { name: '2' })).toBeVisible()
})
