import { expect, test } from '@playwright/test'

test('public detail headings reserve scroll margin under the sticky navbar', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog/seeded-blog')

  const firstLink = page.getByTestId('blog-toc').getByRole('link').first()
  const targetId = (await firstLink.getAttribute('href'))?.replace(/^#/, '')
  expect(targetId).toBeTruthy()

  await firstLink.click()

  const top = await page.locator(`#${targetId}`).evaluate((element) => element.getBoundingClientRect().top)
  expect(top).toBeGreaterThanOrEqual(80)
})
