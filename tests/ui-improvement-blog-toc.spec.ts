import { expect, test } from '@playwright/test'

test('desktop blog detail renders a table of contents and anchors headings', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog/seeded-blog')

  const toc = page.getByTestId('blog-toc')
  await expect(toc).toBeVisible()
  const firstLink = toc.getByRole('link').first()
  await expect(firstLink).toBeVisible()

  const targetId = (await firstLink.getAttribute('href'))?.replace(/^#/, '')
  expect(targetId).toBeTruthy()

  await firstLink.click()
  await expect(page).toHaveURL(new RegExp(`#${targetId}$`))
  await expect(page.locator(`#${targetId}`)).toBeVisible()
})

test('mobile blog detail hides the table of contents', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/blog/seeded-blog')

  await expect(page.getByTestId('blog-toc')).not.toBeVisible()
})
