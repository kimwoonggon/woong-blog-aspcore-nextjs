import { expect, test } from '@playwright/test'

test('navbar no longer renders the duplicate Latest writing link', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 1080 })
  await page.goto('/')

  await expect(page.getByRole('link', { name: 'Latest writing' })).toHaveCount(0)
})

test('primary Blog navigation link still routes to /blog', async ({ page }) => {
  await page.goto('/')

  const blogLink = page.getByRole('link', { name: 'Blog', exact: true }).first()
  await expect(blogLink).toBeVisible()
  await blogLink.click()
  await expect(page).toHaveURL(/\/blog$/)
})
