import { expect, test } from '@playwright/test'

function isMultipleOfFour(value: number) {
  return Math.abs(value % 4) < 0.01
}

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('VA-020 key layout paddings and gaps follow the 4px spacing rhythm', async ({ page }) => {
  await page.goto('/')

  const homeSamples = await Promise.all([
    page.locator('main > div').first().evaluate((element: HTMLElement) => getComputedStyle(element).rowGap),
    page.getByTestId('featured-works-section').evaluate((element: HTMLElement) => getComputedStyle(element).paddingTop),
    page.getByTestId('recent-posts-section').evaluate((element: HTMLElement) => getComputedStyle(element).paddingLeft),
    page.locator('footer nav[aria-label="Footer navigation"]').evaluate((element: HTMLElement) => getComputedStyle(element).columnGap),
  ])

  await page.goto('/admin/blog')

  const adminSamples = await Promise.all([
    page.locator('main').first().evaluate((element: HTMLElement) => getComputedStyle(element).paddingTop),
    page.getByTestId('admin-blog-row').first().evaluate((element: HTMLElement) => getComputedStyle(element).paddingLeft),
    page.locator('aside').first().evaluate((element: HTMLElement) => getComputedStyle(element).paddingLeft),
  ])

  for (const rawValue of [...homeSamples, ...adminSamples]) {
    const value = Number.parseFloat(rawValue)
    expect(isMultipleOfFour(value)).toBe(true)
  }
})
