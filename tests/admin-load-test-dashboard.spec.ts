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

  let loadRequestCount = 0
  await page.route(/\/(?:works|blog)(?:\/[^?]+)?\?__loadTestRun=/, async (route) => {
    loadRequestCount += 1
    await new Promise((resolve) => setTimeout(resolve, loadRequestCount % 2 === 0 ? 900 : 80))
    await route.fulfill({
      status: 200,
      contentType: 'text/html',
      body: '<!doctype html><html><body><main>load test response</main></body></html>',
    })
  })

  await page.goto('/admin/load-test')

  await expect(page.getByRole('heading', { name: 'Load Test Dashboard' })).toBeVisible()
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Load Test' })).toBeVisible()
  await expect(page.getByText('Work list')).toBeVisible()
  await expect(page.getByText('Work read')).toBeVisible()
  await expect(page.getByText('Study list')).toBeVisible()
  await expect(page.getByText('Study read')).toBeVisible()

  await page.getByLabel('Start users').fill('2')
  await page.getByLabel('Max users').fill('2')
  await page.getByLabel('Step users').fill('1')
  await page.getByLabel('Concurrency').fill('2')

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
})
