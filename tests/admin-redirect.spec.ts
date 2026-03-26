import { expect, test } from '@playwright/test'

test('unauthenticated admin access redirects to login', async ({ page }) => {
  await page.goto('/admin/dashboard')

  await expect(page).toHaveURL(/\/login/)
  await expect(page.getByRole('button', { name: 'Sign in with Google' })).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/admin-redirect.png', fullPage: true })
})
