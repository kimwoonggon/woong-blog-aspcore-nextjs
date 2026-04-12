import { expect, test } from '@playwright/test'

test('Recent posts cards keep a visible border', async ({ page }) => {
  await page.goto('/')

  const card = page.getByTestId('recent-post-card').first().locator('[data-slot="card"]').first()
  await expect(card).toBeVisible()
  const borderWidth = await card.evaluate((element) => getComputedStyle(element).borderTopWidth)
  expect(Number.parseFloat(borderWidth)).toBeGreaterThan(0)
})

test('Recent posts tags render as rounded badge-style pills', async ({ page }) => {
  await page.goto('/')

  const badge = page.getByTestId('recent-post-card').first().locator('.rounded-full.bg-muted').first()
  await expect(badge).toBeVisible()
})

test('Recent posts cards advertise hover border and shadow states', async ({ page }) => {
  await page.goto('/')

  const card = page.getByTestId('recent-post-card').first().locator('[data-slot="card"]').first()
  await expect(card).toHaveClass(/hover:border-primary\/30/)
  await expect(card).toHaveClass(/hover:shadow-md/)
})

test('Recent posts titles use card-level group hover accent styling', async ({ page }) => {
  await page.goto('/')

  const card = page.getByTestId('recent-post-card').first()
  await expect(card).toHaveClass(/group/)
  await expect(card.locator('[data-slot="card-title"]').first()).toHaveClass(/group-hover:text-brand-accent/)
})
