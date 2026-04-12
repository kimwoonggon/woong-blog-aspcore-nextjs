import { expect, test } from '@playwright/test'

test('blog detail prev-next keeps the originating related page state', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1600 })
  await page.goto('/blog?page=5&pageSize=1')

  const firstCard = page.getByTestId('blog-card').first()
  await expect(firstCard).toBeVisible()
  await firstCard.click()

  await expect(page).toHaveURL(/relatedPage=5/)
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Posts' }) }).getByText(/^Page 5 of \d+$/)).toBeVisible()

  const nextLink = page.getByTestId('blog-prev-next').getByRole('link', { name: /Next|Previous/ }).first()
  await expect(nextLink).toHaveAttribute('href', /relatedPage=5/)
  await nextLink.click()

  await expect(page).toHaveURL(/relatedPage=5/)
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Posts' }) }).getByText(/^Page 5 of \d+$/)).toBeVisible()
})

test('work detail prev-next keeps the originating related page state', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1600 })
  await page.goto('/works?page=5&pageSize=1')

  const firstCard = page.getByTestId('work-card').first()
  await expect(firstCard).toBeVisible()
  await firstCard.click()

  await expect(page).toHaveURL(/relatedPage=5/)
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Works' }) }).getByText(/^Page 5 of \d+$/)).toBeVisible()

  const nextLink = page.getByTestId('work-prev-next').getByRole('link', { name: /Next|Previous/ }).first()
  await expect(nextLink).toHaveAttribute('href', /relatedPage=5/)
  await nextLink.click()

  await expect(page).toHaveURL(/relatedPage=5/)
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Works' }) }).getByText(/^Page 5 of \d+$/)).toBeVisible()
})
