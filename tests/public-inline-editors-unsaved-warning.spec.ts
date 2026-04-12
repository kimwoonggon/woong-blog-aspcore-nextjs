import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('public blog inline editor clears beforeunload after save', async ({ page }) => {
  const updatedTitle = `Public inline blog save ${Date.now()}`
  let dialogSeen = false

  page.on('dialog', async (dialog) => {
    dialogSeen = true
    await dialog.dismiss()
  })

  await page.goto('/blog?pageSize=1')
  await page.locator('a[href^="/blog/"]').first().click()
  const detailUrlPattern = /\/blog\/[^/?#]+(?:\?.*)?$/
  await page.getByRole('button', { name: '글 수정' }).click()

  await page.getByLabel('Title').fill(updatedTitle)
  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('function')

  await Promise.all([
    page.waitForURL(detailUrlPattern),
    page.waitForResponse((res) => res.url().includes('/api/admin/blogs/') && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: /Update Post/i }).click(),
  ])

  await expect(page.getByText(updatedTitle).first()).toBeVisible()
  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('object')
  expect(dialogSeen).toBe(false)
})

test('public work inline editor clears beforeunload after save', async ({ page }) => {
  const updatedTitle = `Public inline work save ${Date.now()}`
  let dialogSeen = false

  page.on('dialog', async (dialog) => {
    dialogSeen = true
    await dialog.dismiss()
  })

  await page.goto('/works?page=1&pageSize=1')
  await page.locator('a[href^="/works/"]').first().click()
  await expect(page).toHaveURL(/\/works\/[^/?#]+\?returnTo=/)
  const returnTo = new URL(page.url()).searchParams.get('returnTo')
  await page.getByRole('button', { name: '작업 수정' }).click()

  await page.getByLabel('Title').fill(updatedTitle)
  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('function')

  await Promise.all([
    page.waitForURL(returnTo ?? /\/works(?:\?.*)?$/),
    page.waitForResponse((res) => res.url().includes('/api/admin/works/') && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: /Update Work/i }).click(),
  ])

  await expect(page.getByText(updatedTitle).first()).toBeVisible()
  await expect.poll(async () => page.evaluate(() => typeof window.onbeforeunload)).toBe('object')
  expect(dialogSeen).toBe(false)
})
