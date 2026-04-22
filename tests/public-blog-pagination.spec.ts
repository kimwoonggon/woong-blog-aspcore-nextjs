import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'
import { expectedPublicBlogPageSize } from './helpers/responsive-policy'

async function expectPageSize(page: import('./helpers/performance-test').Page, expectedPageSize: string) {
  const currentPageSize = new URL(page.url()).searchParams.get('pageSize')
  if (currentPageSize) {
    expect(currentPageSize).toBe(expectedPageSize)
    return
  }

  await expect(page.locator(`nav[aria-label="Study pagination"] a[href*="pageSize=${expectedPageSize}"]`).first()).toBeVisible()
}

test('blog pagination uses desktop page size and exposes first/prev/next/last controls', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog')

  const pagination = page.getByLabel('Study pagination')

  await expect(pagination).toBeVisible()
  await expect(pagination.getByText(/\d+\s*\/\s*\d+/)).toBeVisible()
  await expectPageSize(page, '12')
  await expect(page.locator('nav[aria-label="Study pagination"] a[href="/blog?page=1&pageSize=12"]').first()).toHaveText('1')
  await expect(pagination.getByText('First')).toBeVisible()
  await expect(pagination.getByText('Previous')).toBeVisible()
  if (await pagination.getByRole('link', { name: 'Next' }).count()) {
    await expect(pagination.getByRole('link', { name: 'Next' })).toHaveAttribute('href', /\/blog\?page=2&pageSize=12$/)
    await expect(pagination.getByRole('link', { name: 'Last' })).toHaveAttribute('href', /\/blog\?page=\d+&pageSize=12$/)
  }
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveClass(/bg-sky-500/)
})

test('blog pagination uses stable tablet page size instead of height-driven collapse', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 720 })
  await page.goto('/blog')

  const expectedPageSize = await expectedPublicBlogPageSize(page)
  await expectPageSize(page, String(expectedPageSize))
  await expect(page.locator(`nav[aria-label="Study pagination"] a[href="/blog?page=1&pageSize=${expectedPageSize}"]`)).toBeVisible()
  await expect(page.getByTestId('blog-card')).toHaveCount(expectedPageSize)
  const compactHeight = await page.getByTestId('blog-card').first().evaluate((element) => element.getBoundingClientRect().height)

  expect(compactHeight).toBeGreaterThan(0)
})

test('blog pagination hydrates page and pageSize query params without rewriting the requested page', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 720 })
  const expectedPageSize = await expectedPublicBlogPageSize(page)
  await page.goto(`/blog?page=2&pageSize=${expectedPageSize}`)

  const pagination = page.getByLabel('Study pagination')
  const pageCountText = await pagination.locator('span').filter({ hasText: /\d+\s*\/\s*\d+/ }).first().innerText()
  test.skip(pageCountText.trim() === '1 / 1', 'Clean seed has one page of study notes.')

  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expectPageSize(page, String(expectedPageSize))
  await expect(pagination.getByText('2 /')).toBeVisible()
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveAttribute('href', `/blog?page=1&pageSize=${expectedPageSize}`)
  await expect(pagination.getByRole('link', { name: 'Previous' })).toHaveAttribute('href', `/blog?page=1&pageSize=${expectedPageSize}`)
  await expect(pagination.getByRole('link', { name: 'Next' })).toHaveAttribute('href', `/blog?page=3&pageSize=${expectedPageSize}`)
  await expect(pagination.getByRole('link', { name: 'Last' })).toHaveAttribute('href', new RegExp(`/blog\\?page=\\d+&pageSize=${expectedPageSize}$`))
  await expect(pagination.getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
  await expect(page.getByTestId('blog-card')).toHaveCount(expectedPageSize)
})

test('blog pagination next and previous clicks stay within latency budget', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  const response = await page.request.get('/api/public/blogs?page=1&pageSize=2')
  expect(response.ok()).toBeTruthy()
  const payload = await response.json() as { totalPages: number }
  test.skip(payload.totalPages < 2, 'Clean seed does not have a second study page.')

  await page.goto('/blog?page=1&pageSize=2&__qaTagged=1')
  await expect(page.getByTestId('blog-card').first()).toBeVisible()

  const pagination = page.getByLabel('Study pagination')
  await measureStep(
    testInfo,
    'Study pagination Next click to active page and grid',
    'publicPagination',
    async () => {
      await Promise.all([
        page.waitForURL((url) => url.searchParams.get('page') === '2'),
        pagination.getByRole('link', { name: 'Next' }).click(),
      ])
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
      await expect(pagination.getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
      await expect(page.getByTestId('blog-card').first()).toBeVisible()
    },
  )

  await measureStep(
    testInfo,
    'Study pagination Previous click to active page and grid',
    'publicPagination',
    async () => {
      await Promise.all([
        page.waitForURL((url) => url.searchParams.get('page') === '1'),
        pagination.getByRole('link', { name: 'Previous' }).click(),
      ])
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('1')
      await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveClass(/bg-sky-500/)
      await expect(page.getByTestId('blog-card').first()).toBeVisible()
    },
  )
})

test('invalid blog page clamps to the nearest valid page', async ({ page, request }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  const expectedPageSize = await expectedPublicBlogPageSize(page)
  const response = await request.get(`/api/public/blogs?page=1&pageSize=${expectedPageSize}`)
  expect(response.ok()).toBeTruthy()
  const payload = await response.json() as { totalPages: number }
  const lastPage = Math.max(1, payload.totalPages)

  await page.goto(`/blog?page=${lastPage + 99}&pageSize=${expectedPageSize}`)

  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe(String(lastPage))
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBe(String(expectedPageSize))
  await expect(page.getByLabel('Study pagination').getByText(`${lastPage} /`)).toBeVisible()
})

test('blog density is stable at tablet width regardless of intermediate height', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 960 })
  await page.goto('/blog')

  const expectedPageSize = await expectedPublicBlogPageSize(page)
  await expectPageSize(page, String(expectedPageSize))
  const cardCount = await page.getByTestId('blog-card').count()
  test.skip(cardCount < 2, 'Clean seed does not have enough study cards for density comparison.')
  expect(cardCount).toBeLessThanOrEqual(expectedPageSize)

  const heights = await page.getByTestId('blog-card').evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(80)
})
