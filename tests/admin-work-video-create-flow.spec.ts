import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

const SAMPLE_MP4 = Buffer.from([
  0x00, 0x00, 0x00, 0x18,
  0x66, 0x74, 0x79, 0x70,
  0x6D, 0x70, 0x34, 0x32,
  0x00, 0x00, 0x00, 0x00,
  0x6D, 0x70, 0x34, 0x32,
  0x69, 0x73, 0x6F, 0x6D,
])

test('admin can create a work with staged YouTube and uploaded videos', async ({ page }) => {
  const title = `Video Create Flow ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.locator('#content').fill('<p>Work with videos</p>')

  await page.getByLabel('YouTube URL or ID').fill('https://youtu.be/dQw4w9WgXcQ')
  await page.getByRole('button', { name: 'Add YouTube Video' }).click()
  await expect(page.getByText('https://youtu.be/dQw4w9WgXcQ')).toBeVisible()

  await page.locator('#work-video-upload').setInputFiles({
    name: 'demo.mp4',
    mimeType: 'video/mp4',
    buffer: SAMPLE_MP4,
  })
  await expect(page.getByText('demo.mp4')).toBeVisible()

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create And Add Videos' }).click(),
  ])

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works(?:/${created.id})?(?:\\?.*)?$`), { timeout: 20000 })

  await page.goto(`/works/${created.slug}`)
  await expect(page.getByRole('heading', { name: title })).toBeVisible()
  await expect(page.locator('iframe[src*="youtube-nocookie.com/embed/dQw4w9WgXcQ"]')).toBeVisible()
  await expect(page.locator('video')).toHaveCount(1, { timeout: 20000 })
})
