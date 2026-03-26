import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('blog editor accepts mixed special-character and Korean input', async ({ page }) => {
  const title = `특수! English 한글 QA ${Date.now()} !!!`

  await page.goto('/admin/blog/new')
  await expect(page).toHaveURL(/\/admin\/blog\/new/)

  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Tags (comma separated)').fill('qa, 한글, !!!, edge-case')
  await page.locator('.tiptap.ProseMirror').first().click()
  await page.keyboard.type('본문 with English, 한국어, and !!! punctuation for extreme input coverage.')

  const [response] = await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/blogs') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create Post' }).click(),
  ])

  expect(response.status()).toBe(200)
  await expect(page).toHaveURL(/\/admin\/blog/)
  await expect(page.getByText(title)).toBeVisible()
})
