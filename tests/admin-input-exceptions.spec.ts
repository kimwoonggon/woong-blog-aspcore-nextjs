import path from 'node:path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin work editor rejects invalid metadata json client-side', async ({ page }) => {
  await page.goto('/admin/works/new')

  await page.getByLabel('Title').fill(`Broken Work ${Date.now()}`)
  await page.getByLabel('Category').fill('qa')
  await page.getByLabel('Flexible Metadata (JSON)').fill('{broken-json')
  await page.getByRole('button', { name: /Create Work/i }).click()

  await expect(page.getByText('Invalid JSON in Flexible Metadata field')).toBeVisible()
  await expect(page).toHaveURL(/\/admin\/works\/new/)
})

test('admin blog editor surfaces save failure when backend rejects input', async ({ page }) => {
  await page.route('**/api/admin/blogs', async (route) => {
    if (route.request().method() !== 'POST') {
      await route.fallback()
      return
    }

    await route.fulfill({
      status: 400,
      contentType: 'text/plain',
      body: 'Save failed from forced playwright route',
    })
  })

  await page.goto('/admin/blog/new')
  await page.getByLabel('Title').fill(`Broken Blog ${Date.now()}`)
  await page.getByLabel('Tags (comma separated)').fill('qa, failure')
  await page.locator('.tiptap.ProseMirror').first().click()
  await page.keyboard.type('Body that should trigger save failure')
  await page.getByRole('button', { name: /Create Post/i }).click()

  await expect(page.getByText('Save failed from forced playwright route')).toBeVisible()
  await expect(page).toHaveURL(/\/admin\/blog\/new/)
})

test('admin resume upload rejects non-pdf files', async ({ page }) => {
  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')

  const resumeSection = page.locator('#resume-editor')
  if (await resumeSection.getByText('Resume PDF Uploaded').isVisible()) {
    await resumeSection.locator('button:has(svg.lucide-trash-2)').click()
    await expect(resumeSection.getByText('No resume uploaded yet.')).toBeVisible()
  }

  const fileInput = resumeSection.locator('#resume-upload')
  await fileInput.setInputFiles(path.resolve('tests/fixtures/not-a-pdf.txt'))

  await expect(page.getByText('Please upload a PDF file.')).toBeVisible()
})
