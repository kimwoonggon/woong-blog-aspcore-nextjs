import { expect, test } from './helpers/performance-test'
import { contrastRatio, getColorChannels, getStyle, gotoWithTheme } from './helpers/ui-improvement'

test('blog cards expose date and tag badge anchors', async ({ page }) => {
  await page.goto('/blog?__qaTagged=1')

  const firstCard = page.getByTestId('blog-card').first()
  await expect(firstCard).toBeVisible()
  await expect(firstCard.locator('[data-slot="badge"]').first()).toBeVisible()
  await expect(firstCard.locator('.rounded-full.bg-muted').first()).toBeVisible()
  await expect(firstCard.getByTestId('blog-card-accent-stripe')).toBeVisible()
})

test('blog tag pills keep accessible contrast in dark mode', async ({ page }) => {
  await gotoWithTheme(page, '/blog?__qaTagged=1')

  const tag = page.locator('[data-testid="blog-card"] .rounded-full.bg-muted').first()
  await expect(tag).toBeVisible()

  const foreground = await getColorChannels(tag, 'color')
  const background = await getColorChannels(tag, 'background-color')
  expect(contrastRatio(foreground, background)).toBeGreaterThanOrEqual(4.5)
})

test('blog grid uses three columns at xl breakpoint', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })
  await page.goto('/blog?__qaTagged=1')

  const grid = page.getByTestId('blog-grid')
  const templateColumns = await getStyle(grid, 'grid-template-columns')
  expect(templateColumns.split(' ').length).toBe(3)
})
