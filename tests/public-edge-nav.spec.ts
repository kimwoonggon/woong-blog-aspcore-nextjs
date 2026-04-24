import { expect, test } from './helpers/performance-test'

test('introduction does not expose left/right edge navigation links', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1080 })
  await page.goto('/introduction')

  await expect(page.getByRole('link', { name: '이전 페이지로 가기' })).toHaveCount(0)
  await expect(page.getByRole('link', { name: '다음 페이지로 가기' })).toHaveCount(0)
})

test('blog edge arrows paginate between blog pages', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  const payload = await (await page.request.get('/api/public/blogs?page=1&pageSize=12')).json() as { totalPages: number }
  test.skip(payload.totalPages < 3, 'Clean seed does not have enough study pages for edge arrows.')
  await page.goto('/blog?page=2&pageSize=10')

  await expect(page.getByRole('link', { name: '이전 페이지로 가기' })).toHaveAttribute('href', '/blog?page=1&pageSize=12')
  await expect(page.getByRole('link', { name: '다음 페이지로 가기' })).toHaveAttribute('href', '/blog?page=3&pageSize=12')
})

test('works edge arrows paginate between work pages', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1800 })
  const payload = await (await page.request.get('/api/public/works?page=1&pageSize=8')).json() as { totalPages: number }
  test.skip(payload.totalPages < 3, 'Clean seed does not have enough work pages for edge arrows.')
  await page.goto('/works?page=2&pageSize=6')

  await expect(page.getByRole('link', { name: '이전 페이지로 가기' })).toHaveAttribute('href', '/works?page=1&pageSize=8')
  await expect(page.getByRole('link', { name: '다음 페이지로 가기' })).toHaveAttribute('href', '/works?page=3&pageSize=8')
})
