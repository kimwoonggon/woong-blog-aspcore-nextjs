import { expect, test } from '@playwright/test'
import { expectedPublicBlogPageSize } from './helpers/responsive-policy'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit a public blog detail inline and return to the originating blog page', async ({ page }) => {
  const updatedTitle = `Inline Blog Title ${Date.now()}`
  const expectedPageSize = await expectedPublicBlogPageSize(page)

  await page.goto(`/blog?page=2&pageSize=${expectedPageSize}`)
  const originalListUrl = page.url()
  await page.locator('a[href^="/blog/"]').first().click()

  await expect(page.getByRole('button', { name: '글 수정' })).toBeVisible()
  await page.getByRole('button', { name: '글 수정' }).click()

  const saveButton = page.getByRole('button', { name: 'Update Post' })
  await expect(saveButton).toBeDisabled()

  await page.getByLabel('Title').fill(updatedTitle)
  await expect(saveButton).toBeEnabled()

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/blogs/') && res.request().method() === 'PUT' && res.ok()),
    saveButton.click(),
  ])

  await expect(page).toHaveURL(originalListUrl)
  await expect(page).not.toHaveURL(/\/admin\/blog\//)
  await expect(page.getByLabel('Title')).toHaveCount(0)
  await expect(page.getByRole('button', { name: '새 글 쓰기' })).toBeVisible()
})

test('public blog detail shows paginated related posts', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog?pageSize=1')
  await page.locator('a[href^="/blog/"]').first().click()

  await expect(page.getByRole('heading', { name: 'More Studies' })).toBeVisible()
  await expect(page.getByTestId('related-blog-card').first()).toBeVisible()
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Studies' }) }).getByText(/^Page 1 of \d+$/)).toBeVisible()
  await expect(page.getByRole('button', { name: 'Next' })).toBeVisible()
  await expect(page.getByRole('button', { name: '2' })).toBeVisible()
})
