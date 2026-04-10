import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can add videos inline while editing an existing work', async ({ page }) => {
  const title = `Video Edit Flow ${Date.now()}`
  const editor = page.locator('.tiptap.ProseMirror').first()

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await editor.fill('Edit mode inline intro.')

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create Work' }).click(),
  ])

  const created = await createResponse.json()
  await page.goto(`/admin/works/${created.id}`)

  await page.getByLabel('YouTube URL or ID').fill('https://youtu.be/dQw4w9WgXcQ')
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/videos/youtube') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Add YouTube Video' }).click(),
  ])

  await page.getByRole('button', { name: 'Insert Into Body' }).nth(0).click()
  await editor.click()
  await editor.pressSequentially(' Tail copy after inline video.')

  await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === `/api/admin/works/${created.id}` && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Work' }).click(),
  ])

  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]').first()).toBeVisible()
  await expect(page.getByText(/Edit mode inline intro\./).first()).toBeVisible()
  await expect(page.getByText(/Tail copy after inline video\./).first()).toBeVisible()
})
