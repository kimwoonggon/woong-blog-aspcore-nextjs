import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin dashboard shows summary cards with numeric counts', async ({ page }) => {
  await page.goto('/admin/dashboard')

  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
  await expect(page.getByText('Total Views')).toBeVisible()
  await expect(page.getByText('Total Works')).toBeVisible()
  await expect(page.getByText('Total Blog Posts')).toBeVisible()
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Members' })).toBeVisible()

  const numberCards = page.locator('main .grid').first().locator('.text-2xl.font-bold')
  await expect(numberCards).toHaveCount(3)
  for (let i = 0; i < 3; i += 1) {
    await expect(numberCards.nth(i)).toContainText(/\d+/)
  }

  await expect(page.getByRole('heading', { name: 'Works' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible()
  await expect(page.getByText(/shown \/ .* total · 클릭하면 바로 편집 페이지로 이동합니다\./).first()).toBeVisible()
})

test('admin dashboard cards open edit pages directly and expose pagination controls', async ({ page }) => {
  await page.goto('/admin/dashboard')

  await expect(page.getByRole('heading', { name: 'Works' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible()
  await expect(page.getByText('이전').first()).toBeVisible()
  await expect(page.getByText('다음').first()).toBeVisible()

  const firstWorkCard = page.getByTestId('works-card-link').first()
  await expect(firstWorkCard).toBeVisible()
  await Promise.all([
    page.waitForURL(/\/admin\/works\//),
    firstWorkCard.click(),
  ])

  await page.goto('/admin/dashboard')

  const firstBlogCard = page.getByTestId('blog-posts-card-link').first()
  await expect(firstBlogCard).toBeVisible()
  await Promise.all([
    page.waitForURL(/\/admin\/blog\//),
    firstBlogCard.click(),
  ])
})

test('admin sidebar exposes a direct public-home shortcut', async ({ page }) => {
  await page.goto('/admin/dashboard')

  await page.getByRole('link', { name: 'Public Home' }).click()
  await expect(page).toHaveURL(/\/$/)
})
