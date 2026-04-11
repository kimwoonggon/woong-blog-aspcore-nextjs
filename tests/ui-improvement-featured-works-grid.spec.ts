import { expect, test } from '@playwright/test'
import { getStyle } from './helpers/ui-improvement'

test('Featured works renders as a grid card layout', async ({ page }) => {
  await page.goto('/')

  const section = page.getByTestId('featured-works-section')
  const grid = page.getByTestId('featured-works-grid')
  await expect(section.getByTestId('featured-work-card').first()).toBeVisible()

  const templateColumns = await getStyle(grid, 'grid-template-columns')
  expect(templateColumns).not.toBe('none')
})

test('clicking a featured work card opens its detail page', async ({ page }) => {
  await page.goto('/')

  const firstCard = page.getByTestId('featured-work-card').first()

  await firstCard.click()
  await expect(page).toHaveURL(/\/works\/.+/)
})

test('View all from Featured works navigates to /works', async ({ page }) => {
  await page.goto('/')

  const section = page.getByRole('heading', { name: 'Featured works' }).locator('xpath=ancestor::section[1]')
  await section.getByRole('link', { name: 'View all' }).click()
  await expect(page).toHaveURL(/\/works$/)
})

test('featured work cards advertise hover interactions', async ({ page }) => {
  await page.goto('/')

  const card = page.getByTestId('featured-work-card').first()

  const image = card.locator('img').first()
  const title = card.locator('h3').first()

  await expect(image).toHaveClass(/group-hover:scale-105/)
  await expect(title).toHaveClass(/group-hover:text-brand-accent/)
})

test('Featured works collapses to one column on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 })
  await page.goto('/')

  const cards = page.getByTestId('featured-work-card')
  await expect(cards.nth(0)).toBeVisible()
  await expect(cards.nth(1)).toBeVisible()

  const firstBox = await cards.nth(0).boundingBox()
  const secondBox = await cards.nth(1).boundingBox()
  expect(firstBox).toBeTruthy()
  expect(secondBox).toBeTruthy()
  expect(Math.abs(firstBox!.x - secondBox!.x)).toBeLessThan(4)
  expect(secondBox!.y).toBeGreaterThan(firstBox!.y)
})

test('Featured works uses two columns on tablet and three on desktop', async ({ page }) => {
  const measureColumns = async (width: number, height: number) => {
    await page.setViewportSize({ width, height })
    await page.goto('/')

    const grid = page.getByTestId('featured-works-grid')

    const templateColumns = await getStyle(grid, 'grid-template-columns')
    return templateColumns.split(' ').length
  }

  expect(await measureColumns(768, 1024)).toBe(2)
  expect(await measureColumns(1280, 800)).toBe(3)
})
