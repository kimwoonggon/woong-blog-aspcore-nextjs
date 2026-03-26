import { expect, test } from '@playwright/test'

test('public home page renders primary navigation', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('link', { name: 'Home' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Works' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Blog' })).toBeVisible()
})
