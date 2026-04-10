import { readFileSync } from 'node:fs'
import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })
const SAMPLE_VIDEO = readFileSync(path.resolve('tests/fixtures/sample-video.mp4'))

test('admin can create a mixed work with two youtube links, two mp4 uploads, and rich body copy', async ({ page }) => {
  const title = `Playwright Mixed Video ${Date.now()}`
  const editor = page.locator('.tiptap.ProseMirror').first()

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.getByLabel('Project Period').fill('2026.04')
  await page.getByLabel('Tags (comma separated)').fill('video, youtube, mp4, mixed')
  await editor.fill('Playwright Mixed Media. This work intentionally mixes uploaded MP4 clips with YouTube embeds.')

  await page.getByLabel('YouTube URL or ID').fill('dQw4w9WgXcQ')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await expect(page.getByText('dQw4w9WgXcQ')).toBeVisible()

  await page.locator('#work-video-upload').setInputFiles({
    name: 'blue-demo.mp4',
    mimeType: 'video/mp4',
    buffer: SAMPLE_VIDEO,
  })
  await expect(page.getByText('blue-demo.mp4')).toBeVisible()

  await page.getByLabel('YouTube URL or ID').fill('9bZkp7q19f0')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await expect(page.getByText('9bZkp7q19f0')).toBeVisible()

  await page.locator('#work-video-upload').setInputFiles({
    name: 'red-demo.mp4',
    mimeType: 'video/mp4',
    buffer: SAMPLE_VIDEO,
  })
  await expect(page.getByText('red-demo.mp4')).toBeVisible()

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create And Add Videos' }).click(),
  ])

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works/${created.id}\\?videoInline=1$`), { timeout: 20000 })
  await expect(page.getByText('Videos were saved. Continue by placing them inline inside the body wherever they should appear.')).toBeVisible()

  await editor.click()
  await editor.press('Control+A')
  await editor.press('Backspace')
  await editor.fill('Before inline video.')

  await page.getByRole('button', { name: 'Insert Into Body' }).nth(0).click()
  await editor.click()
  await editor.pressSequentially(' Between videos.')
  await page.getByRole('button', { name: 'Insert Into Body' }).nth(1).click()
  await editor.click()
  await editor.pressSequentially(' More inline video.')
  await page.getByRole('button', { name: 'Insert Into Body' }).nth(2).click()
  await editor.click()
  await editor.pressSequentially(' Final inline video.')
  await page.getByRole('button', { name: 'Insert Into Body' }).nth(3).click()
  await editor.click()
  await editor.pressSequentially(' After inline video.')

  await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === `/api/admin/works/${created.id}` && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Work' }).click(),
  ])

  await page.goto(`/works/${created.slug}`)
  await expect(page.getByRole('heading', { name: title })).toBeVisible()
  await expect(page.getByText(/Before inline video\./).first()).toBeVisible()
  await expect(page.getByText(/Between videos\./).first()).toBeVisible()
  await expect(page.getByText(/More inline video\./).first()).toBeVisible()
  await expect(page.getByText(/Final inline video\./).first()).toBeVisible()
  await expect(page.getByText(/After inline video\./).first()).toBeVisible()
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]')).toBeVisible()
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/9bZkp7q19f0"]')).toBeVisible()
  await expect(page.locator('video')).toHaveCount(2, { timeout: 20000 })
})
