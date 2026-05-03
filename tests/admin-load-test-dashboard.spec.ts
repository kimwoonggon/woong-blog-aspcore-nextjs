import { expect, test } from './helpers/performance-test'
import { createBlogFixture, createWorkFixture } from './helpers/content-fixtures'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can run a small Work and Study read load test', async ({ page, request }, testInfo) => {
  await createWorkFixture(request, testInfo, {
    titlePrefix: 'Load Test Work Fixture',
    html: '<p>Work read load fixture body.</p>',
  })
  await createBlogFixture(request, testInfo, {
    titlePrefix: 'Load Test Study Fixture',
    html: '<p>Study read load fixture body.</p>',
  })

  const loadRequestUrls: string[] = []
  const loadRequestCookies: Array<string | undefined> = []
  let loadRequestCount = 0
  await page.route(/\/api\/public\/(?:works|blogs)(?:\/[^?]+)?(?:\?[^#]*)?__loadTestRun=/, async (route) => {
    loadRequestCount += 1
    loadRequestUrls.push(route.request().url())
    loadRequestCookies.push(route.request().headers().cookie)
    await new Promise((resolve) => setTimeout(resolve, loadRequestCount % 2 === 0 ? 350 : 80))
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: '{"items":[],"page":1,"pageSize":1,"totalItems":0,"totalPages":0}',
    })
  })

  await page.goto('/admin/load-test')

  await expect(page.getByRole('heading', { name: 'Load Test Dashboard' })).toBeVisible()
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Load Test' })).toBeVisible()
  await expect(page.getByText('Work list', { exact: true })).toBeVisible()
  await expect(page.getByText('Work read', { exact: true })).toBeVisible()
  await expect(page.getByText('Study list', { exact: true })).toBeVisible()
  await expect(page.getByText('Study read', { exact: true })).toBeVisible()
  await expect(page.getByLabel('Work read URL')).toHaveValue(/\/api\/public\/works\//)
  await expect(page.getByLabel('Study read URL')).toHaveValue(/\/api\/public\/blogs\//)

  await page.getByLabel('Start users').fill('2')
  await page.getByLabel('Max users').fill('2')
  await page.getByLabel('Step users').fill('1')
  await page.getByLabel('Concurrency').fill('2')
  await page.getByLabel('Work read URL').fill('/api/public/works/custom-work-target')
  await page.getByLabel('Study read URL').fill('/api/public/blogs/custom-study-target')

  await page.getByRole('button', { name: 'Run load test' }).click()

  await expect(page.getByTestId('load-test-live-status')).toContainText(/running/i)
  await expect(page.getByTestId('load-test-summary-table')).toBeVisible()
  await expect(page.getByTestId('load-test-summary-table')).toContainText('Running')
  await expect(page.getByTestId('load-test-summary-table')).toContainText('1 / 2')

  await expect(page.getByTestId('load-test-summary-table')).toContainText('Work list')
  await expect(page.getByTestId('load-test-summary-table')).toContainText('Completed', { timeout: 15000 })
  await expect(page.getByTestId('load-test-summary-table')).toContainText('2 / 2')
  await expect(page.getByTestId('load-test-summary-table')).toContainText('Study list')
  await expect(page.getByTestId('load-test-result-count')).toContainText(/4 scenarios/)
  expect(loadRequestUrls.some((url) => url.includes('/api/public/works/custom-work-target?'))).toBe(true)
  expect(loadRequestUrls.some((url) => url.includes('/api/public/blogs/custom-study-target?'))).toBe(true)
  expect(loadRequestUrls.some((url) => url.includes('__loadTestUser=1'))).toBe(true)
  expect(loadRequestUrls.some((url) => url.includes('__loadTestUser=2'))).toBe(true)
  expect(loadRequestCookies.every((cookie) => !cookie)).toBe(true)
})
