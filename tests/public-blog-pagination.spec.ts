import { expect, test } from '@playwright/test'

async function expectPageSize(page: import('@playwright/test').Page, expectedPageSize: string) {
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBe(expectedPageSize)
}

test('blog pagination uses desktop page size and exposes navigation controls', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog')

  await expect(page.getByLabel('Blog pagination')).toBeVisible()
  await expect(page.getByLabel('Blog pagination').getByText(/\d+\s*\/\s*\d+/)).toBeVisible()
  await expectPageSize(page, '12')
  await expect(page.locator('nav[aria-label="Blog pagination"] a[href="/blog?page=1&pageSize=12"]')).toBeVisible()
  await expect(page.getByLabel('Blog pagination').getByText('이전')).toBeVisible()
  await expect(page.getByLabel('Blog pagination').getByText('다음')).toBeVisible()
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
