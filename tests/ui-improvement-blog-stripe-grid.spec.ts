import { expect, test } from '@playwright/test'
import { getStyle } from './helpers/ui-improvement'

test('blog cards expose the accent stripe visual anchor', async ({ page }) => {
  await page.goto('/blog')

  const firstCard = page.getByTestId('blog-card').first()
  await expect(firstCard).toBeVisible()
  await expect(firstCard.getByTestId('blog-card-accent-stripe')).toBeVisible()
})

test('blog grid uses three columns on xl desktop', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog')

  const grid = page.getByTestId('blog-grid')
  const templateColumns = await getStyle(grid, 'grid-template-columns')
  expect(templateColumns.split(' ').length).toBe(3)
})
