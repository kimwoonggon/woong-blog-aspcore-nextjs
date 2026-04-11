import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('blog editor registers beforeunload only after a real change', async ({ page }) => {
  await page.goto('/admin/blog/new')

  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('object')

  await page.getByLabel('Title').fill(`Dirty blog ${Date.now()}`)
  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('function')
})

test('work editor registers beforeunload only after a real change', async ({ page }) => {
  await page.goto('/admin/works/new')

  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('object')

  await page.getByLabel('Title').fill(`Dirty work ${Date.now()}`)
  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('function')
})
