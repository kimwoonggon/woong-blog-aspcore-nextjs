import { expect, test, type Page } from '@playwright/test'

async function expectVisibleHeaderMetadata(page: Page) {
  const title = (await page.locator('article header h1').innerText()).trim()
  const description = (await page.locator('article header p').first().innerText()).trim()

  await expect.poll(() => page.title()).toContain(title)
  await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', description)
}

test('blog detail metadata uses the visible article title and excerpt', async ({ page }) => {
  await page.goto('/blog/seeded-blog')
  await expectVisibleHeaderMetadata(page)
})

test('work detail metadata uses the visible project title and excerpt', async ({ page }) => {
  await page.goto('/works/seeded-work')
  await expectVisibleHeaderMetadata(page)
})
