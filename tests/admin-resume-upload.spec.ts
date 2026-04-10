import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin can upload a resume pdf and public resume page exposes download', async ({ page }) => {
  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  const resumeSection = page.locator('#resume-editor')
  await expect(resumeSection.getByText('Resume Management')).toBeVisible()

  if (await resumeSection.getByText('Resume PDF Uploaded').isVisible()) {
    await resumeSection.locator('button:has(svg.lucide-trash-2)').click()
    await expect(resumeSection.getByText('No resume uploaded yet.')).toBeVisible()
  }

  const fileInput = resumeSection.locator('#resume-upload')
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/uploads') && res.request().method() === 'POST' && res.ok()),
    fileInput.setInputFiles(path.resolve('tests/fixtures/resume.pdf')),
  ])

  await expect(resumeSection.getByText('Resume PDF Uploaded')).toBeVisible({ timeout: 20000 })

  await page.goto('/resume')
  const downloadLink = page.getByRole('link', { name: /download/i }).first()
  await expect(downloadLink).toBeVisible()
  await expect(downloadLink).toHaveAttribute('href', /\/media\/public-resume\//)

  const [download] = await Promise.all([
    page.waitForEvent('download'),
    downloadLink.click(),
  ])
  expect(await download.suggestedFilename()).toMatch(/\.pdf$/i)

  await page.getByRole('button', { name: '이력서 PDF 업로드' }).click()
  await expect(page.getByText('Resume Management')).toBeVisible()
  await page.locator('button:has(svg.lucide-trash-2)').click()
  await expect(page.locator('main')).toContainText('No resume uploaded yet.', { timeout: 20000 })
  await expect(page.getByRole('link', { name: /download/i })).toHaveCount(0)

  await page.goto('/admin/pages')
  await expect(page.locator('#resume-editor').getByText('No resume uploaded yet.')).toBeVisible()
})
