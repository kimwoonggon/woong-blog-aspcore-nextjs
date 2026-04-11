import { expect, test } from '@playwright/test'

test('home container stays within max-w-7xl on large screens', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 1080 })
  await page.goto('/')

  const container = page.locator('main > div.container').first()
  const width = await container.evaluate((element) => element.getBoundingClientRect().width)
  expect(width).toBeLessThanOrEqual(1280)
})

test('home and blog pages share the same container width on desktop', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 1080 })

  await page.goto('/')
  const homeWidth = await page.locator('main > div.container').first().evaluate((element) => element.getBoundingClientRect().width)

  await page.goto('/blog')
  const blogWidth = await page.locator('main > div.container').first().evaluate((element) => element.getBoundingClientRect().width)

  expect(Math.abs(homeWidth - blogWidth)).toBeLessThanOrEqual(1)
})
