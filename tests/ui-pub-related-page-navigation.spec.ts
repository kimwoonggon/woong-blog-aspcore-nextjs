import { expect, test } from './helpers/performance-test'

test('blog detail prev-next keeps the originating related page state', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1600 })
  await page.goto('/blog?page=5&pageSize=1')

  const firstCard = page.getByTestId('blog-card').first()
  await expect(firstCard).toBeVisible()
  await firstCard.click()

  await expect(page).toHaveURL(/relatedPage=\d+/)
  const relatedPage = new URL(page.url()).searchParams.get('relatedPage')
  expect(relatedPage).toBeTruthy()
  await expect(page.getByTestId('blog-related-shell')).toBeVisible()

  const nextLink = page.getByTestId('blog-prev-next').getByRole('link', { name: /Next|Previous/ }).first()
  await expect(nextLink).toHaveAttribute('href', new RegExp(`relatedPage=${relatedPage}`))
  await nextLink.click()

  await expect(page).toHaveURL(new RegExp(`relatedPage=${relatedPage}`))
  await expect(page.getByTestId('blog-related-shell')).toBeVisible()
})

test('work detail prev-next keeps the originating related page state', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1600 })
  await page.goto('/works?page=5&pageSize=1')

  const firstCard = page.getByTestId('work-card').first()
  await expect(firstCard).toBeVisible()
  await firstCard.click()

  await expect(page).toHaveURL(/relatedPage=\d+/)
  const relatedPage = new URL(page.url()).searchParams.get('relatedPage')
  expect(relatedPage).toBeTruthy()
  await expect(page.getByTestId('work-related-shell')).toBeVisible()

  const nextLink = page.getByTestId('work-prev-next').getByRole('link', { name: /Next|Previous/ }).first()
  await expect(nextLink).toHaveAttribute('href', new RegExp(`relatedPage=${relatedPage}`))
  await nextLink.click()

  await expect(page).toHaveURL(new RegExp(`relatedPage=${relatedPage}`))
  await expect(page.getByTestId('work-related-shell')).toBeVisible()
})
