import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('home page editor persists mixed Korean, English, and punctuation input', async ({ page }) => {
  const headline = `안녕! Extreme Home ${Date.now()} !!!`
  const introText = `영어 English / 한국어 / !!! / ??? / () [] {} / edge ${Date.now()}`

  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  const homeEditor = page.getByRole('heading', { name: 'Home Page - Hero Section' }).locator('xpath=ancestor::div[contains(@class, "space-y-6")][1]')
  await homeEditor.getByLabel('Headline').fill(headline)
  await homeEditor.getByLabel('Intro Text').fill(introText)

  await Promise.all([
    page.waitForResponse((res) =>
      res.url().includes('/api/admin/pages')
      && res.request().method() === 'PUT'
      && (res.request().postData() ?? '').includes(headline)
      && res.ok(),
    ),
    homeEditor.getByRole('button', { name: 'Save Changes' }).click(),
  ])

  await page.goto('/admin/pages')
  await expect(page.getByRole('heading', { name: 'Home Page - Hero Section' }).locator('xpath=ancestor::div[contains(@class, "space-y-6")][1]').getByLabel('Headline')).toHaveValue(headline)
  await expect(page.getByRole('heading', { name: 'Home Page - Hero Section' }).locator('xpath=ancestor::div[contains(@class, "space-y-6")][1]').getByLabel('Intro Text')).toHaveValue(introText)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: headline })).toBeVisible()
  await expect(page.getByText(introText)).toBeVisible()
})
