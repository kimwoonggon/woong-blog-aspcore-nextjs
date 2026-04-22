import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test('works search filters cards by title', async ({ page }, testInfo) => {
  await measureStep(
    testInfo,
    'Works search title query to filtered cards',
    'publicSearch',
    async () => {
      await page.goto('/works?query=Portfolio%20Platform&searchMode=title&page=1&pageSize=8')
    },
    async () => {
      await expect(page.getByLabel('Search work')).toHaveValue('Portfolio Platform')
      await expect(page.getByLabel('Work search mode')).toHaveValue('title')
      await expect(page.getByTestId('work-card').first()).toContainText('Portfolio Platform Rebuild')
    },
  )
})

test('works search supports content mode and empty results', async ({ page }, testInfo) => {
  await measureStep(
    testInfo,
    'Works search content query to empty state',
    'publicSearch',
    async () => {
      await page.goto('/works?query=no-such-work-token&searchMode=content&page=1&pageSize=8')
    },
    async () => {
      await expect(page.getByLabel('Search work')).toHaveValue('no-such-work-token')
      await expect(page.getByLabel('Work search mode')).toHaveValue('content')
      await expect(page.getByText('No works found.')).toBeVisible()
    },
  )
})
