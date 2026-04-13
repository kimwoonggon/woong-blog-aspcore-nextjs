import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('public work detail renders uploaded and youtube videos when present', async ({ page }) => {
  const title = `Public Video Flow ${Date.now()}`
  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.locator('.tiptap.ProseMirror').first().fill('Public video work')
  await page.getByLabel('YouTube URL or ID').fill('dQw4w9WgXcQ')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await page.locator('#work-video-upload').setInputFiles(path.resolve('tests/fixtures/sample-video.mp4'))

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create with Videos' }).click(),
  ])

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works/${created.id}\\?videoInline=1$`), { timeout: 20000 })

  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]')).toBeVisible()
  await expect(page.locator('video')).toHaveCount(1, { timeout: 20000 })
})

test('PF-041 public work detail lets visitors play and pause an uploaded video', async ({ page }) => {
  const title = `Public Video Playback ${Date.now()}`
  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.locator('.tiptap.ProseMirror').first().fill('Playback coverage for an uploaded public video')
  await page.locator('#work-video-upload').setInputFiles(path.resolve('tests/fixtures/sample-video.mp4'))

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: /Create with 1 Video|Create with Videos/ }).click(),
  ])

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works/${created.id}\\?videoInline=1$`), { timeout: 20000 })

  await page.goto(`/works/${created.slug}`)
  const video = page.locator('video').first()
  await expect(video).toBeVisible()

  await video.evaluate(async (node) => {
    const media = node as HTMLVideoElement
    media.muted = true
    await media.play()
  })

  await expect
    .poll(async () => {
      return video.evaluate((node) => {
        const media = node as HTMLVideoElement
        return {
          paused: media.paused,
          currentTime: media.currentTime,
          readyState: media.readyState,
        }
      })
    })
    .toMatchObject({
      paused: false,
      readyState: expect.any(Number),
    })

  await expect
    .poll(async () => video.evaluate((node) => (node as HTMLVideoElement).currentTime))
    .toBeGreaterThan(0)

  await video.evaluate((node) => {
    ;(node as HTMLVideoElement).pause()
  })
  await expect.poll(async () => video.evaluate((node) => (node as HTMLVideoElement).paused)).toBe(true)
})

test('PF-043 public work detail keeps public video render order aligned with saved sort order', async ({ page }) => {
  const title = `Public Video Order ${Date.now()}`
  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.locator('.tiptap.ProseMirror').first().fill('Render order coverage for public work videos')

  await page.getByLabel('YouTube URL or ID').fill('dQw4w9WgXcQ')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await page.locator('#work-video-upload').setInputFiles(path.resolve('tests/fixtures/sample-video.mp4'))
  await page.getByLabel('YouTube URL or ID').fill('9bZkp7q19f0')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: /Create with 3 Videos|Create with Videos/ }).click(),
  ])

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works/${created.id}\\?videoInline=1$`), { timeout: 20000 })

  await page.goto(`/works/${created.slug}`)

  const leadVideo = page.getByTestId('work-lead-video')
  await expect(leadVideo).toBeVisible()
  await expect(leadVideo.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]')).toBeVisible()

  const moreVideos = page.getByTestId('work-more-videos')
  await moreVideos.getByText(/More videos \(2\)/).click()

  const orderedMedia = moreVideos.locator('iframe, video')
  await expect(orderedMedia).toHaveCount(2)
  await expect(orderedMedia.nth(0)).toHaveJSProperty('tagName', 'VIDEO')
  await expect(orderedMedia.nth(1)).toHaveAttribute('src', /youtube-nocookie\.com\/embed\/9bZkp7q19f0/)
})
