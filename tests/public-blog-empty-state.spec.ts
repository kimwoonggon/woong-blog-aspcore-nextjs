import { expect, test } from '@playwright/test'

test('PF-014 shows an empty-state message when no blog posts are available', async ({ page }) => {
  await page.goto('/blog?__qaEmpty=1')

  await expect(page.getByRole('heading', { name: 'Blog', exact: true })).toBeVisible()
  await expect(page.getByTestId('blog-card')).toHaveCount(0)
  await expect(page.locator('main')).toContainText('No blog posts found.')
  await expect(page.getByLabel('Blog pagination')).toBeVisible()
  await expect(page.getByLabel('Blog pagination').getByText('1 / 1')).toBeVisible()
})
