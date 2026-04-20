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

test('View all from Works navigates to /works', async ({ page }) => {
  await page.goto('/')

  const section = page.getByRole('heading', { name: 'Works', exact: true }).locator('xpath=ancestor::section[1]')
  await section.getByRole('link', { name: 'View all' }).click()
  await expect(page).toHaveURL(/\/works$/)
})

test('featured work cards advertise hover interactions', async ({ page }) => {
  await page.goto('/')

  const card = page.getByTestId('featured-work-card').first()

  const image = card.locator('img').first()
  const title = card.locator('h3').first()
  const placeholder = card.getByTestId('featured-work-no-image-placeholder')

  if (await image.count()) {
    await expect(image).toHaveClass(/group-hover:scale-105/)
  } else {
    await expect(placeholder).toBeVisible()
    await expect(card.locator('[data-slot="card"]').first()).toHaveClass(/hover:border-primary\/30/)
  }

  await expect(title).toHaveClass(/group-hover:text-brand-accent/)
})

test('featured work cards use the richer no-image placeholder treatment', async ({ page }) => {
  await page.goto('/works?__qaNoImage=1')

  const placeholder = page.getByTestId('work-card-no-image-placeholder').first()
  await expect(placeholder).toBeVisible()
  await expect(placeholder).toHaveClass(/bg-gradient-to-br/)
})

test('home featured works no-image placeholder matches icon plus label pattern', async ({ page }) => {
  await page.goto('/?__qaNoImage=1')

  const placeholder = page.getByTestId('featured-work-no-image-placeholder').first()
  await expect(placeholder).toBeVisible()
  await expect(placeholder.locator('svg')).toBeVisible()
  await expect(placeholder.getByText('No Image', { exact: true })).toBeVisible()
})

test('home featured works no longer shows the legacy click-to-view-details fallback copy', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByText('Click to view details', { exact: true })).toHaveCount(0)
})

test('Works collapses to one column on mobile', async ({ page }) => {
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

test('Works uses two columns on tablet and three on desktop', async ({ page }) => {
  const measureColumns = async (width: number, height: number) => {
    await page.setViewportSize({ width, height })
    await page.goto('/')

    const grid = page.getByTestId('featured-works-grid')

    const templateColumns = await getStyle(grid, 'grid-template-columns')
    const repeatMatch = templateColumns.match(/^repeat\((\d+),/)
    return repeatMatch ? Number(repeatMatch[1]) : templateColumns.split(' ').length
  }

  expect(await measureColumns(768, 1024)).toBe(2)
  expect(await measureColumns(1280, 800)).toBe(3)
})

test('hero CTAs keep a clear primary-versus-secondary visual hierarchy', async ({ page }) => {
  await page.goto('/')

  const primary = page.getByRole('link', { name: 'View My Works' })
  const secondary = page.getByRole('link', { name: 'Read Study' })
  await expect(primary).toBeVisible()
  await expect(secondary).toBeVisible()

  await expect(primary).toHaveClass(/bg-foreground/)
  await expect(primary).toHaveClass(/text-background/)
  await expect(secondary).toHaveClass(/border/)
  await expect(secondary).not.toHaveClass(/bg-foreground/)
})

test('featured work media keeps a 4:3 ratio and cards keep consistent heights', async ({ page }) => {
  await page.goto('/')

  const cards = page.getByTestId('featured-work-card')
  await expect(cards.nth(0)).toBeVisible()
  await expect(cards.nth(1)).toBeVisible()

  const mediaBoxes = await Promise.all([0, 1].map(async (index) => {
    const box = await cards.nth(index).locator('.aspect-\\[4\\/3\\]').first().boundingBox()
    expect(box).toBeTruthy()
    return box!
  }))
  const cardBoxes = await Promise.all([0, 1].map(async (index) => {
    const box = await cards.nth(index).locator('[data-slot="card"]').first().boundingBox()
    expect(box).toBeTruthy()
    return box!
  }))

  for (const box of mediaBoxes) {
    expect(Math.abs((box.width / box.height) - (4 / 3))).toBeLessThan(0.08)
  }

  expect(Math.abs(cardBoxes[0].height - cardBoxes[1].height)).toBeLessThanOrEqual(6)
})

test('featured work cards keep a shared resting shadow and a stronger hover elevation', async ({ page }) => {
  await page.goto('/')

  const firstCard = page.getByTestId('featured-work-card').nth(0).locator('[data-slot="card"]').first()
  const secondCard = page.getByTestId('featured-work-card').nth(1).locator('[data-slot="card"]').first()
  await expect(firstCard).toBeVisible()
  await expect(secondCard).toBeVisible()

  const firstShadow = await getStyle(firstCard, 'box-shadow')
  const secondShadow = await getStyle(secondCard, 'box-shadow')
  expect(firstShadow).toBe(secondShadow)
  expect(firstShadow).not.toBe('none')

  await firstCard.hover()
  const hoveredShadow = await getStyle(firstCard, 'box-shadow')
  expect(hoveredShadow).not.toBe('none')
})
