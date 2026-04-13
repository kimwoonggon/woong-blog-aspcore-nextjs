import { expect, test } from '@playwright/test'

async function expectPageSize(page: import('@playwright/test').Page, expectedPageSize: string) {
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBe(expectedPageSize)
}

test('works pagination uses desktop page size and exposes first/prev/next/last controls', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/works')

  const pagination = page.getByLabel('Works pagination')

  await expect(pagination).toBeVisible()
  await expect(pagination.getByText(/\d+\s*\/\s*\d+/)).toBeVisible()
  await expectPageSize(page, '8')
  await expect(page.locator('nav[aria-label="Works pagination"] a[href="/works?page=1&pageSize=8"]').first()).toHaveText('1')
  await expect(pagination.getByText('처음')).toBeVisible()
  await expect(pagination.getByText('이전')).toBeVisible()
  await expect(pagination.getByRole('link', { name: '다음' })).toHaveAttribute('href', /\/works\?page=2&pageSize=8$/)
  await expect(pagination.getByRole('link', { name: '마지막' })).toHaveAttribute('href', /\/works\?page=\d+&pageSize=8$/)
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveClass(/bg-sky-500/)
})

test('works pagination comes up earlier on shorter viewports by reducing the page size', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 720 })
  await page.goto('/works')

  await expectPageSize(page, '2')
  await expect(page.locator('nav[aria-label="Works pagination"] a[href="/works?page=1&pageSize=2"]')).toBeVisible()
  const compactHeight = await page.getByTestId('work-card').first().evaluate((element) => element.getBoundingClientRect().height)

  expect(compactHeight).toBeLessThan(500)
})

test('works pagination hydrates page and pageSize query params without rewriting the requested page', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 720 })
  await page.goto('/works?page=2&pageSize=2')

  const pagination = page.getByLabel('Works pagination')

  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expectPageSize(page, '2')
  await expect(pagination.getByText('2 /')).toBeVisible()
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveAttribute('href', '/works?page=1&pageSize=2')
  await expect(pagination.getByRole('link', { name: '이전' })).toHaveAttribute('href', '/works?page=1&pageSize=2')
  await expect(pagination.getByRole('link', { name: '다음' })).toHaveAttribute('href', '/works?page=3&pageSize=2')
  await expect(pagination.getByRole('link', { name: '마지막' })).toHaveAttribute('href', /\/works\?page=\d+&pageSize=2$/)
  await expect(pagination.getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
  await expect(page.getByTestId('work-card')).toHaveCount(2)
})

test('works density changes smoothly at intermediate heights', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/works')

  await expectPageSize(page, '3')
  await expect(page.getByTestId('work-card')).toHaveCount(3)

  const heights = await page.getByTestId('work-card').evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(4)
})
