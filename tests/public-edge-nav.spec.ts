import { expect, test } from '@playwright/test'

test('introduction does not expose left/right edge navigation links', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1080 })
  await page.goto('/introduction')

  await expect(page.getByRole('link', { name: '이전 페이지로 가기' })).toHaveCount(0)
  await expect(page.getByRole('link', { name: '다음 페이지로 가기' })).toHaveCount(0)
})

test('blog edge arrows paginate between blog pages', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/blog?page=2&pageSize=10')

  await expect(page.getByRole('link', { name: '이전 페이지로 가기' })).toHaveAttribute('href', '/blog?page=1&pageSize=10')
  await expect(page.getByRole('link', { name: '다음 페이지로 가기' })).toHaveAttribute('href', '/blog?page=3&pageSize=10')
})

test('works edge arrows paginate between work pages', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  await page.goto('/works?page=2&pageSize=6')

  await expect(page.getByRole('link', { name: '이전 페이지로 가기' })).toHaveAttribute('href', '/works?page=1&pageSize=6')
  await expect(page.getByRole('link', { name: '다음 페이지로 가기' })).toHaveAttribute('href', '/works?page=3&pageSize=6')
})
