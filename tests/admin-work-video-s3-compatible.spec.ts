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

test('s3-compatible lane uses presigned upload and renders the uploaded video publicly', async ({ page }) => {
  const title = `S3 Video Flow ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.locator('#content').fill('<p>S3-backed video work</p>')
  await page.locator('#work-video-upload').setInputFiles({
    name: 'demo.mp4',
    mimeType: 'video/mp4',
    buffer: SAMPLE_MP4,
  })

  const [createResponse, uploadTargetResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.waitForResponse((res) => res.url().includes('/videos/upload-url') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create And Add Videos' }).click(),
  ])

  const uploadTarget = await uploadTargetResponse.json()
  expect(uploadTarget.uploadMethod).toBe('PUT')

  const created = await createResponse.json()
  await page.waitForURL(/\/admin\/works(?:\?.*)?$/, { timeout: 20000 })
  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('video')).toHaveCount(1, { timeout: 20000 })
})
