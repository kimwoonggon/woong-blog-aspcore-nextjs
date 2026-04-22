import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'
import { expectedPublicWorksPageSize } from './helpers/responsive-policy'

test('works pagination uses desktop page size and exposes first/prev/next/last controls', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/works')

  const pagination = page.getByLabel('Works pagination')

  await expect(pagination).toBeVisible()
  await expect(pagination.getByText(/\d+\s*\/\s*\d+/)).toBeVisible()
  await expect(page.locator('nav[aria-label="Works pagination"] a[href="/works?page=1&pageSize=8"]').first()).toHaveText('1')
  await expect(pagination.getByText('First')).toBeVisible()
  await expect(pagination.getByText('Previous')).toBeVisible()
  await expect(pagination.getByRole('link', { name: 'Next' })).toHaveAttribute('href', /\/works\?page=2&pageSize=8$/)
  await expect(pagination.getByRole('link', { name: 'Last' })).toHaveAttribute('href', /\/works\?page=\d+&pageSize=8$/)
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveClass(/bg-sky-500/)
})

test('works pagination uses stable tablet page size instead of height-driven collapse', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 720 })
  await page.goto('/works')

  const expectedPageSize = await expectedPublicWorksPageSize(page)
  await expect(page.getByTestId('work-card')).toHaveCount(expectedPageSize)
  await expect(page.locator(`nav[aria-label="Works pagination"] a[href="/works?page=1&pageSize=${expectedPageSize}"]`)).toBeVisible()
})

test('works pagination hydrates page and pageSize query params without rewriting the requested page', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 720 })
  const expectedPageSize = await expectedPublicWorksPageSize(page)
  await page.goto(`/works?page=2&pageSize=${expectedPageSize}`)

  const pagination = page.getByLabel('Works pagination')

  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expect(pagination.getByText('2 /')).toBeVisible()
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveAttribute('href', `/works?page=1&pageSize=${expectedPageSize}`)
  await expect(pagination.getByRole('link', { name: 'Previous' })).toHaveAttribute('href', `/works?page=1&pageSize=${expectedPageSize}`)
  await expect(pagination.getByRole('link', { name: 'Next' })).toHaveAttribute('href', `/works?page=3&pageSize=${expectedPageSize}`)
  await expect(pagination.getByRole('link', { name: 'Last' })).toHaveAttribute('href', new RegExp(`/works\\?page=\\d+&pageSize=${expectedPageSize}$`))
  await expect(pagination.getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
  await expect(page.getByTestId('work-card')).toHaveCount(expectedPageSize)
})

test('works pagination next and previous clicks stay within latency budget', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  const response = await page.request.get('/api/public/works?page=1&pageSize=2')
  expect(response.ok()).toBeTruthy()
  const payload = await response.json() as { totalPages: number }
  test.skip(payload.totalPages < 2, 'Clean seed does not have a second works page.')

  await page.goto('/works?page=1&pageSize=2')
  await expect(page.getByTestId('work-card').first()).toBeVisible()

  const pagination = page.getByLabel('Works pagination')
  await measureStep(
    testInfo,
    'Works pagination Next click to active page and grid',
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
      await expect(page.getByTestId('work-card').first()).toBeVisible()
    },
  )

  await measureStep(
    testInfo,
    'Works pagination Previous click to active page and grid',
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
      await expect(page.getByTestId('work-card').first()).toBeVisible()
    },
  )
})

test('works density is stable at tablet width regardless of intermediate height', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 960 })
  await page.goto('/works')

  const expectedPageSize = await expectedPublicWorksPageSize(page)
  await expect(page.getByTestId('work-card')).toHaveCount(expectedPageSize)

  const heights = await page.getByTestId('work-card').evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(4)
})
