import { expect, test } from '@playwright/test'

async function expectPageSize(page: import('@playwright/test').Page, expectedPageSize: string) {
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBe(expectedPageSize)
}

test('blog pagination uses desktop page size and exposes first/prev/next/last controls', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog')

  const pagination = page.getByLabel('Blog pagination')

  await expect(pagination).toBeVisible()
  await expect(pagination.getByText(/\d+\s*\/\s*\d+/)).toBeVisible()
  await expectPageSize(page, '12')
  await expect(page.locator('nav[aria-label="Blog pagination"] a[href="/blog?page=1&pageSize=12"]').first()).toHaveText('1')
  await expect(pagination.getByText('처음')).toBeVisible()
  await expect(pagination.getByText('이전')).toBeVisible()
  await expect(pagination.getByRole('link', { name: '다음' })).toHaveAttribute('href', /\/blog\?page=2&pageSize=12$/)
  await expect(pagination.getByRole('link', { name: '마지막' })).toHaveAttribute('href', /\/blog\?page=\d+&pageSize=12$/)
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveClass(/bg-sky-500/)
})

test('blog pagination comes up earlier on shorter viewports by reducing the page size', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 720 })
  await page.goto('/blog')

  await expectPageSize(page, '2')
  await expect(page.locator('nav[aria-label="Blog pagination"] a[href="/blog?page=1&pageSize=2"]')).toBeVisible()
  await expect(page.getByTestId('blog-card')).toHaveCount(2)
  const compactHeight = await page.getByTestId('blog-card').first().evaluate((element) => element.getBoundingClientRect().height)

  expect(compactHeight).toBeLessThan(260)
})

test('blog pagination hydrates page and pageSize query params without rewriting the requested page', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 720 })
  await page.goto('/blog?page=2&pageSize=2')

  const pagination = page.getByLabel('Blog pagination')

  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expectPageSize(page, '2')
  await expect(pagination.getByText('2 /')).toBeVisible()
  await expect(pagination.getByRole('link', { name: '1', exact: true })).toHaveAttribute('href', '/blog?page=1&pageSize=2')
  await expect(pagination.getByRole('link', { name: '이전' })).toHaveAttribute('href', '/blog?page=1&pageSize=2')
  await expect(pagination.getByRole('link', { name: '다음' })).toHaveAttribute('href', '/blog?page=3&pageSize=2')
  await expect(pagination.getByRole('link', { name: '마지막' })).toHaveAttribute('href', /\/blog\?page=\d+&pageSize=2$/)
  await expect(pagination.getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
  await expect(page.getByTestId('blog-card')).toHaveCount(2)
})

test('blog density changes smoothly at intermediate heights', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/blog')

  await expectPageSize(page, '4')
  await expect(page.getByTestId('blog-card')).toHaveCount(4)

  const heights = await page.getByTestId('blog-card').evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(80)
})
