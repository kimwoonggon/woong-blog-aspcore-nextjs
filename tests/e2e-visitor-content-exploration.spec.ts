import { expect, test } from '@playwright/test'

test('E2E-003 visitor can move from home to works, through related content, and into blog details', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('heading', { name: /Featured works/i })).toBeVisible()
  await page.getByRole('banner').getByRole('link', { name: 'Works', exact: true }).click()
  await expect(page).toHaveURL(/\/works(?:\?.*)?$/)

  const firstWorkCard = page.getByTestId('work-card').first()
  await expect(firstWorkCard).toBeVisible()
  const firstWorkTitle = (await firstWorkCard.locator('h2').innerText()).trim()
  await firstWorkCard.click()

  await expect(page.locator('main h1')).toHaveText(firstWorkTitle)
  await expect(page.getByTestId('work-related-shell')).toBeVisible()

  const relatedWorkCard = page.getByTestId('related-work-card').first()
  await expect(relatedWorkCard).toBeVisible()
  await relatedWorkCard.click()

  await expect(page.locator('main h1')).toBeVisible()
  await expect(page).toHaveURL(/\/works\/.+/)

  await page.getByRole('banner').getByRole('link', { name: 'Blog', exact: true }).click()
  await expect(page).toHaveURL(/\/blog(?:\?.*)?$/)

  const firstBlogCard = page.getByTestId('blog-card').first()
  await expect(firstBlogCard).toBeVisible()
  const firstBlogTitle = (await firstBlogCard.locator('[data-slot="card-title"]').innerText()).trim()
  await firstBlogCard.click()

  await expect(page.locator('main h1')).toHaveText(firstBlogTitle)
  await expect(page.getByTestId('blog-related-shell')).toBeVisible()
  await expect(page.getByTestId('blog-prev-next')).toBeVisible()
})
