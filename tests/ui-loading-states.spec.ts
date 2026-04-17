import { expect, test } from '@playwright/test'
import { clickHeaderNavLink, rewriteHeaderNavHref } from './helpers/navigation'

test('WQ-024 and VA-403 public route transitions expose a loading skeleton before content resolves', async ({ page }) => {
  await page.goto('/blog')
  await rewriteHeaderNavHref(page, 'Home', '/?__qaSlow=1')
  await clickHeaderNavLink(page, 'Home')

  const skeleton = page.locator('.animate-pulse').first()
  await expect(skeleton).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Works', exact: true })).toBeVisible()
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
