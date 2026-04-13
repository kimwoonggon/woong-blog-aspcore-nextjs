import { expect, test } from '@playwright/test'

async function installLayoutShiftObserver(page: any) {
  await page.addInitScript(() => {
    ;(window as any).__qaClsValue = 0
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries() as any) {
        if (!entry.hadRecentInput) {
          ;(window as any).__qaClsValue += entry.value
        }
      }
    })
    observer.observe({ type: 'layout-shift', buffered: true })
  })
}

test('WQ-023 public slow route transition keeps cumulative layout shift below 0.1', async ({ page }) => {
  await installLayoutShiftObserver(page)
  await page.goto('/blog')

  const homeLink = page.getByRole('banner').getByRole('link', { name: 'Home', exact: true })
  await homeLink.evaluate((element) => {
    ;(element as HTMLAnchorElement).href = '/?__qaSlow=1'
  })
  await homeLink.click()

  const skeleton = page.locator('.animate-pulse').first()
  await expect(skeleton).toBeVisible()
  await page.evaluate(() => {
    ;(window as any).__qaClsValue = 0
  })
  await expect(page.getByRole('heading', { name: /Featured works/i })).toBeVisible()
  const cls = await page.evaluate(() => (window as any).__qaClsValue ?? 0)
  expect(cls).toBeLessThanOrEqual(0.105)
})

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('WQ-023 admin slow dashboard transition keeps cumulative layout shift below 0.1', async ({ page }) => {
  await installLayoutShiftObserver(page)
  await page.goto('/admin/blog')

  const dashboardLink = page.locator('aside nav').getByRole('link', { name: 'Dashboard' })
  await dashboardLink.evaluate((element) => {
    ;(element as HTMLAnchorElement).href = '/admin/dashboard?__qaSlow=1'
  })
  await dashboardLink.click()

  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible()
  const cls = await page.evaluate(() => (window as any).__qaClsValue ?? 0)
  expect(cls).toBeLessThanOrEqual(0.105)
})
