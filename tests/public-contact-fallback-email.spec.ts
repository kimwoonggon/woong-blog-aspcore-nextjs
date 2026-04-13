import { expect, test } from '@playwright/test'

test('PF-061 shows the fallback email when the contact content has no mailto link', async ({ page }) => {
  await page.route('**/api/public/pages/contact', async (route) => {
    const response = await route.fetch()
    const payload = await response.json() as {
      id: string
      slug: string
      title: string
      contentJson: string
    }

    await route.fulfill({
      response,
      contentType: 'application/json',
      body: JSON.stringify({
        ...payload,
        contentJson: JSON.stringify({
          html: '<p>Please use the direct fallback email below for project inquiries.</p>',
        }),
      }),
    })
  })

  await page.goto('/contact')

  await expect(page.getByRole('heading', { name: 'Contact', exact: true })).toBeVisible()
  await expect(page.locator('main a[href="mailto:woong@example.com"]')).toBeVisible()
  await expect(page.locator('main')).toContainText('Direct email')
  await expect(page.locator('main')).toContainText('woong@example.com')
  await expect(page.locator('main a[href="mailto:john@example.com"]')).toHaveCount(0)
})
