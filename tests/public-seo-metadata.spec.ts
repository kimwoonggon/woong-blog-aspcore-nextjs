import { expect, test, type Page } from '@playwright/test'

async function expectVisibleHeaderMetadata(page: Page) {
  const title = (await page.locator('article header h1').innerText()).trim()

  await expect.poll(() => page.title()).toContain(title)
  await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', /.+/)
}

test('blog detail metadata uses the visible article title and excerpt', async ({ page }) => {
  await page.goto('/blog/seeded-blog')
  await expectVisibleHeaderMetadata(page)
})

test('work detail metadata uses the visible project title and excerpt', async ({ page }) => {
  await page.goto('/works/seeded-work')
  await expectVisibleHeaderMetadata(page)
})

test('site exposes a branded svg favicon', async ({ page, request }) => {
  await page.goto('/')

  const icon = page.locator('link[rel~="icon"]').first()
  await expect(icon).toHaveAttribute('href', '/favicon.svg')

  const response = await request.get('/favicon.svg')
  expect(response.ok()).toBeTruthy()
  expect(response.headers()['content-type']).toContain('image/svg+xml')
  await expect(response.text()).resolves.toContain('WK')
})
