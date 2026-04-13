import { expect, test } from '@playwright/test'

test('WQ-024 and VA-403 public route transitions expose a loading skeleton before content resolves', async ({ page }) => {
  await page.goto('/blog')
  const homeLink = page.getByRole('banner').getByRole('link', { name: 'Home', exact: true })
  await homeLink.evaluate((element) => {
    ;(element as HTMLAnchorElement).href = '/?__qaSlow=1'
  })
  await homeLink.click()

  const skeleton = page.locator('.animate-pulse').first()
  await expect(skeleton).toBeVisible()
  await expect(page.getByRole('heading', { name: /Featured works/i })).toBeVisible()
})

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('WQ-024 admin dashboard transitions expose loading chrome before the dashboard resolves', async ({ page }) => {
  await page.goto('/admin/blog')

  const dashboardLink = page.locator('aside nav').getByRole('link', { name: 'Dashboard' })
  await dashboardLink.evaluate((element) => {
    ;(element as HTMLAnchorElement).href = '/admin/dashboard?__qaSlow=1'
  })
  await dashboardLink.click()

  const skeleton = page.locator('.animate-pulse').first()
  await expect(skeleton).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible()
})
