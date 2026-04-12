import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('resume page exposes a download action', async ({ page }) => {
  await page.goto('/resume')
  await expect(page.getByRole('heading', { name: 'Resume', exact: true })).toBeVisible()

  const downloadLink = page.getByRole('link', { name: /Download/i })
  if (!(await downloadLink.isVisible())) {
    const request = page.context().request
    const csrfResponse = await request.get('/api/auth/csrf')
    const csrf = await csrfResponse.json() as { requestToken: string; headerName: string }

    const uploadResponse = await request.post('/api/uploads', {
      headers: {
        [csrf.headerName]: csrf.requestToken,
      },
      multipart: {
        bucket: 'public-resume',
        file: {
          name: 'resume.pdf',
          mimeType: 'application/pdf',
          buffer: await (await import('node:fs/promises')).readFile(path.resolve('tests/fixtures/resume.pdf')),
        },
      },
    })
    expect(uploadResponse.ok()).toBeTruthy()
    const uploadData = await uploadResponse.json() as { id: string }

    const linkResponse = await request.put('/api/admin/site-settings', {
      headers: {
        'Content-Type': 'application/json',
        [csrf.headerName]: csrf.requestToken,
      },
      data: {
        resumeAssetId: uploadData.id,
      },
    })
    expect(linkResponse.ok()).toBeTruthy()

    await page.goto('/resume')
  }

  await expect(page.getByRole('heading', { name: 'Resume', exact: true })).toBeVisible()
  await expect(downloadLink).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/resume-page.png', fullPage: true })
})
