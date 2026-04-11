import { expect, test } from '@playwright/test'

test('introduction and contact pages keep the public reading layout', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })

  await page.goto('/introduction')
  await expect(page.locator('main h1').first()).toBeVisible()
  const introWidth = await page.getByTestId('static-public-shell').evaluate((element) => element.getBoundingClientRect().width)
  expect(introWidth).toBeLessThanOrEqual(896)

  await page.goto('/contact')
  await expect(page.locator('main h1').first()).toBeVisible()
  await expect(page.locator('main a[href^="mailto:"]').first()).toBeVisible()
  const contactWidth = await page.getByTestId('static-public-shell').evaluate((element) => element.getBoundingClientRect().width)
  expect(contactWidth).toBeLessThanOrEqual(896)
})

test('resume page keeps a tall viewer shell without clipping the document area', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })
  await page.goto('/resume')

  await expect(page.locator('main h1').first()).toBeVisible()
  const shellHeight = await page.getByTestId('resume-shell').evaluate((element) => element.getBoundingClientRect().height)
  expect(shellHeight).toBeGreaterThanOrEqual(540)
})
