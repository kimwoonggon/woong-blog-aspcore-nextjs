import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('s3-compatible lane uses presigned upload and renders the uploaded video publicly', async ({ page }) => {
  const title = `S3 Video Flow ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('video')
  await page.locator('.tiptap.ProseMirror').first().fill('S3-backed video work')
  await page.locator('#work-video-upload').setInputFiles(path.resolve('tests/fixtures/sample-video.mp4'))

  const [createResponse, uploadTargetResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.waitForResponse((res) => res.url().includes('/videos/upload-url') && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: 'Create And Add Videos' }).click(),
  ])

  const uploadTarget = await uploadTargetResponse.json()
  expect(['PUT', 'POST']).toContain(uploadTarget.uploadMethod)

  const created = await createResponse.json()
  await page.waitForURL(new RegExp(`/admin/works/${created.id}\\?videoInline=1$`), { timeout: 20000 })
  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('video')).toHaveCount(1, { timeout: 20000 })
})
