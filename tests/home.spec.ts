import { expect, test } from '@playwright/test'

test('public home page renders primary navigation', async ({ page }) => {
  await page.goto('/')

  const headerNav = page.getByRole('banner').getByRole('navigation')
  await expect(headerNav.getByRole('link', { name: 'Home', exact: true })).toBeVisible()
  await expect(headerNav.getByRole('link', { name: 'Works', exact: true })).toBeVisible()
  await expect(headerNav.getByRole('link', { name: 'Blog', exact: true })).toBeVisible()
})
