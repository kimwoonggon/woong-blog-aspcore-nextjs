import { expect, test } from '@playwright/test'

test('PF-061 shows the fallback email when the contact content has no mailto link', async ({ page }) => {
  await page.goto('/contact?__qaNoMailto=1')

  await expect(page.getByRole('heading', { name: 'Contact', exact: true })).toBeVisible()
  await expect(page.locator('main a[href="mailto:woong@example.com"]')).toBeVisible()
  await expect(page.locator('main')).toContainText('Direct email')
  await expect(page.locator('main')).toContainText('woong@example.com')
  await expect(page.locator('main a[href="mailto:john@example.com"]')).toHaveCount(0)
})
