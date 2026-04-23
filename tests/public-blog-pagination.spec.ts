import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test('study list uses infinite feed on mobile (390px)', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/blog')

  await expect(page.getByLabel('Study pagination')).toBeHidden()
  await expect(page.getByTestId('blog-card')).toHaveCount(10)
  await expect(page.getByTestId('blog-load-more')).toBeVisible()
})

test('study list uses infinite feed on tablet (820px) and appends next page', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 820, height: 1180 })
  await page.goto('/blog')

  await expect(page.getByLabel('Study pagination')).toBeHidden()
  await expect.poll(() => page.getByTestId('blog-card').count()).toBeGreaterThanOrEqual(10)
  const initialCount = await page.getByTestId('blog-card').count()

  await measureStep(
    testInfo,
    'Study tablet load more appends next page',
    'publicPagination',
    async () => {
      await page.getByTestId('blog-load-more').click()
    },
    async () => {
      await expect.poll(() => page.getByTestId('blog-card').count()).toBeGreaterThan(initialCount)
    },
  )
})

test('study list keeps desktop pagination and hides infinite controls at >=1024px', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/blog')

  const pagination = page.getByLabel('Study pagination')
  await expect(pagination).toBeVisible()
  await expect(page.getByTestId('blog-load-more')).toHaveCount(0)
  const payload = await (await page.request.get('/api/public/blogs?page=1&pageSize=12')).json() as { totalPages: number }
  test.skip(payload.totalPages < 2, 'Clean seed does not have a second study page.')
  await expect(pagination.getByRole('link', { name: 'Next' })).toBeVisible()

  await measureStep(
    testInfo,
    'Study desktop pagination next navigation',
    'publicPagination',
    async () => {
      await pagination.getByRole('link', { name: 'Next' }).click()
      await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
    },
    async () => {
      await expect(page.getByLabel('Study pagination').getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
    },
  )
})
