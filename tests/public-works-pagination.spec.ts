import { expect, test } from '@playwright/test'

async function expectPageSize(page: import('@playwright/test').Page, expectedPageSize: string) {
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBe(expectedPageSize)
}

test('works pagination uses desktop page size and exposes navigation controls', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/works')

  await expect(page.getByLabel('Works pagination')).toBeVisible()
  await expect(page.getByLabel('Works pagination').getByText(/\d+\s*\/\s*\d+/)).toBeVisible()
  await expectPageSize(page, '8')
  await expect(page.locator('nav[aria-label="Works pagination"] a[href="/works?page=1&pageSize=8"]')).toBeVisible()
  await expect(page.getByLabel('Works pagination').getByText('이전')).toBeVisible()
  await expect(page.getByLabel('Works pagination').getByText('다음')).toBeVisible()
})

test('works pagination comes up earlier on shorter viewports by reducing the page size', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 720 })
  await page.goto('/works')

  await expectPageSize(page, '2')
  await expect(page.locator('nav[aria-label="Works pagination"] a[href="/works?page=1&pageSize=2"]')).toBeVisible()
  const compactHeight = await page.getByTestId('work-card').first().evaluate((element) => element.getBoundingClientRect().height)

  expect(compactHeight).toBeLessThan(500)
})

test('works density changes smoothly at intermediate heights', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/works')

  await expectPageSize(page, '3')
  await expect(page.getByTestId('work-card')).toHaveCount(3)

  const heights = await page.getByTestId('work-card').evaluateAll((elements) =>
    elements.map((element) => Math.round(element.getBoundingClientRect().height))
  )

  expect(Math.max(...heights) - Math.min(...heights)).toBeLessThan(80)
})
