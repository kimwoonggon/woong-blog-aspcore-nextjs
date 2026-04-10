import { expect, test } from '@playwright/test'

test('public home page renders primary navigation', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('link', { name: 'Home', exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Works', exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Blog', exact: true })).toBeVisible()
})
