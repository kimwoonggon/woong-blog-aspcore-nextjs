import { expect, test } from './helpers/performance-test'

test('login page renders and sign-in CTA targets backend auth launcher', async ({ page }) => {
  await page.goto('/login')

  const button = page.getByRole('link', { name: 'Sign in with Google' })
  await expect(button).toBeVisible()

  await button.click()
  await page.waitForURL(/\/api\/auth\/login|accounts\.google\.com/)
  await page.screenshot({ path: 'test-results/playwright/auth-login-page.png', fullPage: true })
})
