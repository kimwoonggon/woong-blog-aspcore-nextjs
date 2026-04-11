import { expect, test } from '@playwright/test'

test('home container stays within max-w-7xl on large screens', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 1080 })
  await page.goto('/')
  await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible()

  const container = page.locator('main > div.container.max-w-7xl').first()
  const width = await container.evaluate((element) => element.getBoundingClientRect().width)
  expect(width).toBeLessThanOrEqual(1280)
})

test('home and blog pages share the same container width on desktop', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 1080 })

  await page.goto('/')
  await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible()
  const homeContainer = page.locator('main > div.container.max-w-7xl').first()
  await expect(homeContainer).toBeVisible()
  const homeWidth = await homeContainer.evaluate((element) => element.getBoundingClientRect().width)

  await page.goto('/blog')
  await expect(page.getByRole('heading', { name: 'Blog', exact: true })).toBeVisible()
  const blogContainer = page.locator('main > div.container.max-w-7xl').first()
  await expect(blogContainer).toBeVisible()
  const blogWidth = await blogContainer.evaluate((element) => element.getBoundingClientRect().width)

  expect(Math.abs(homeWidth - blogWidth)).toBeLessThanOrEqual(4)
})
