import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

const BLUE_MP4 = Buffer.from([
  0x00, 0x00, 0x00, 0x18,
  0x66, 0x74, 0x79, 0x70,
  0x6D, 0x70, 0x34, 0x32,
  0x00, 0x00, 0x00, 0x00,
  0x6D, 0x70, 0x34, 0x32,
  0x69, 0x73, 0x6F, 0x6D,
])

const RED_MP4 = Buffer.from([
  0x00, 0x00, 0x00, 0x18,
  0x66, 0x74, 0x79, 0x70,
  0x6D, 0x70, 0x34, 0x32,
  0x00, 0x00, 0x00, 0x00,
  0x6D, 0x70, 0x34, 0x32,
  0x69, 0x73, 0x6F, 0x6D,
])

test('admin can create a mixed work with two youtube links, two mp4 uploads, and rich body copy', async ({ page }) => {
  const title = `Playwright Mixed Video ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.getByLabel('Project Period').fill('2026.04')
  await page.getByLabel('Tags (comma separated)').fill('video, youtube, mp4, mixed')
  await page.locator('#content').fill(
    '<h2>Playwright Mixed Media</h2><p>This work intentionally mixes uploaded MP4 clips with YouTube embeds.</p><p>The goal is to leave a browser recording and prove the public page renders both sources together.</p>'
  )

  await page.getByLabel('YouTube URL or ID').fill('dQw4w9WgXcQ')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await expect(page.getByText('dQw4w9WgXcQ')).toBeVisible()

  await page.locator('#work-video-upload').setInputFiles({
    name: 'blue-demo.mp4',
    mimeType: 'video/mp4',
    buffer: BLUE_MP4,
  })
  await expect(page.getByText('blue-demo.mp4')).toBeVisible()

  await page.getByLabel('YouTube URL or ID').fill('9bZkp7q19f0')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await expect(page.getByText('9bZkp7q19f0')).toBeVisible()

  await page.locator('#work-video-upload').setInputFiles({
    name: 'red-demo.mp4',
    mimeType: 'video/mp4',
    buffer: RED_MP4,
  })
  await expect(page.getByText('red-demo.mp4')).toBeVisible()

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create And Add Videos' }).click(),
  ])

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works(?:/${created.id})?(?:\\?.*)?$`), { timeout: 20000 })

  await page.goto(`/works/${created.slug}`)
  await expect(page.getByRole('heading', { name: title })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Playwright Mixed Media' })).toBeVisible()
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]')).toBeVisible()
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/9bZkp7q19f0"]')).toBeVisible()
  await expect(page.locator('video')).toHaveCount(2, { timeout: 20000 })
})
