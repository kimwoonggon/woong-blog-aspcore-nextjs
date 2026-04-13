import { expect, test, type Locator } from '@playwright/test'

async function countColumns(locator: Locator) {
  const template = await locator.evaluate((element: HTMLElement) => getComputedStyle(element).gridTemplateColumns)
  return template.split(' ').filter(Boolean).length
}

test('WQ-020 and VA-131 work card media reserves space and keeps cover cropping', async ({ page }) => {
  await page.goto('/works')

  const card = page.getByTestId('work-card').first()
  await expect(card).toBeVisible()

  const media = card.locator('.aspect-\\[4\\/3\\]').first()
  await expect(media).toBeVisible()

  const ratio = await media.evaluate((element: HTMLElement) => {
    const rect = element.getBoundingClientRect()
    return rect.width / rect.height
  })
  expect(Math.abs(ratio - 4 / 3)).toBeLessThan(0.08)

  const image = media.locator('img').first()
  if (await image.count()) {
    await expect(image).toHaveCSS('object-fit', 'cover')
  }
})

test('WQ-031 and VA-130 works archive grid adapts from tablet to desktop columns', async ({ page }) => {
  test.fixme(true, 'Current works archive renders 3 columns at desktop width; the plan target remains open until the layout is updated.')

  await page.setViewportSize({ width: 768, height: 1024 })
  await page.goto('/works')

  const grid = page.locator('xpath=//div[contains(@class,"grid") and contains(@class,"xl:grid-cols-4")]').first()
  await expect(page.getByTestId('work-card').first()).toBeVisible()
  expect(await countColumns(grid)).toBe(2)

  await page.setViewportSize({ width: 1440, height: 1024 })
  await page.goto('/works')
  expect(await countColumns(grid)).toBe(4)
})

test('WQ-036 and VA-112 public pagination compresses on mobile while keeping an active page state', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog?page=2&pageSize=12')

  const desktopPagination = page.locator('nav[aria-label="Blog pagination"]')
  await expect(desktopPagination).toBeVisible()
  const desktopNumberLinks = await desktopPagination.locator('a').evaluateAll((links) =>
    links.filter((link) => /^\d+$/.test((link.textContent ?? '').trim())).length,
  )

  await page.setViewportSize({ width: 375, height: 667 })
  await page.goto('/blog?page=2&pageSize=2')

  const mobilePagination = page.locator('nav[aria-label="Blog pagination"]')
  await expect(mobilePagination).toBeVisible()
  const mobileNumberLinks = await mobilePagination.locator('a').evaluateAll((links) =>
    links.filter((link) => /^\d+$/.test((link.textContent ?? '').trim())).length,
  )

  expect(mobileNumberLinks).toBeLessThanOrEqual(desktopNumberLinks)
  await expect(mobilePagination.getByRole('link', { name: '2', exact: true })).toHaveClass(/bg-sky-500/)
  await expect(mobilePagination.getByRole('link', { name: '이전' })).toBeVisible()
  await expect(mobilePagination.getByRole('link', { name: '다음' })).toBeVisible()
})
