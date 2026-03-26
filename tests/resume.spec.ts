import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('resume page exposes a download action', async ({ page }) => {
  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/resume')
  await expect(page.getByRole('heading', { name: 'Resume', exact: true })).toBeVisible()

  const downloadLink = page.getByRole('link', { name: /Download/i })
  if (!(await downloadLink.isVisible())) {
    await page.goto('/admin/pages')

    const resumeSection = page.locator('#resume-editor')
    if (await resumeSection.getByText('Resume PDF Uploaded').isVisible()) {
      await resumeSection.locator('button:has(svg.lucide-trash-2)').click()
      await expect(resumeSection.getByText('No resume uploaded yet.')).toBeVisible()
    }

    await resumeSection.locator('#resume-upload').setInputFiles(path.resolve('tests/fixtures/resume.pdf'))
    await expect(resumeSection.getByText('Resume PDF Uploaded')).toBeVisible({ timeout: 20000 })
    await page.goto('/resume')
  }

  await expect(page.getByRole('heading', { name: 'Resume', exact: true })).toBeVisible()
  await expect(downloadLink).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/resume-page.png', fullPage: true })
})
