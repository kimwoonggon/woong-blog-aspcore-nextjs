import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('home page editor persists mixed Korean, English, and punctuation input', async ({ page }) => {
  const headline = `안녕! Extreme Home ${Date.now()} !!!`
  const introText = `영어 English / 한국어 / !!! / ??? / () [] {} / edge ${Date.now()}`

  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  await page.getByLabel('Headline').fill(headline)
  await page.getByLabel('Intro Text').fill(introText)

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/pages') && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Save Changes' }).nth(1).click(),
  ])

  await page.goto('/')
  await expect(page.getByRole('heading', { name: headline })).toBeVisible()
  await expect(page.getByText(introText)).toBeVisible()
})
