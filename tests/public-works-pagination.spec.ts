import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test('works list uses infinite feed on mobile (390px)', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/works')

  await expect(page.getByLabel('Works pagination')).toBeHidden()
  await expect(page.getByTestId('work-card')).toHaveCount(10)
  await expect(page.getByTestId('works-load-more')).toBeVisible()
})

test('works list uses infinite feed on tablet (820px) and appends next page', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 820, height: 1180 })
  await page.goto('/works')

  const firstPagePayload = await (await page.request.get('/api/public/works?page=1&pageSize=10')).json() as {
    items: unknown[]
  }
  const secondPagePayload = await (await page.request.get('/api/public/works?page=2&pageSize=10')).json() as {
    items: unknown[]
  }
  test.skip(secondPagePayload.items.length === 0, 'Current seed has no second page to append in infinite mode.')

  const expectedInitialCount = firstPagePayload.items.length
  const expectedCountAfterAppend = expectedInitialCount + secondPagePayload.items.length

  await expect(page.getByLabel('Works pagination')).toBeHidden()
  await expect(page.getByTestId('work-card')).toHaveCount(expectedInitialCount)

  await measureStep(
    testInfo,
    'Works tablet load more appends next page',
    'publicPagination',
    async () => {
      await page.getByTestId('works-load-more').click()
    },
    async () => {
      await expect.poll(async () => page.getByTestId('work-card').count()).toBeGreaterThan(expectedInitialCount)

      const countAfterAppend = await page.getByTestId('work-card').count()
      expect(countAfterAppend).toBeLessThanOrEqual(expectedCountAfterAppend)
    },
  )
})

test('works list keeps desktop pagination and hides infinite controls at >=1024px', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/works')

  const pagination = page.getByLabel('Works pagination')
  await expect(pagination).toBeVisible()
  await expect(page.getByTestId('works-load-more')).toHaveCount(0)
  const payload = await (await page.request.get('/api/public/works?page=1&pageSize=8')).json() as { totalPages: number }
  test.skip(payload.totalPages < 2, 'Clean seed does not have a second works page.')
  await expect(pagination.getByRole('link', { name: 'Next' })).toBeVisible()

  await measureStep(
    testInfo,
    'Works desktop pagination next navigation',
    'publicPagination',
    async () => {
      await pagination.getByRole('link', { name: 'Next' }).click()
      await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
    },
    async () => {
      await expect(page.getByLabel('Works pagination').getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
    },
  )
})
