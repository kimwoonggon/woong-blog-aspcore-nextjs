import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('home page editor uploads a profile image and public home renders it', async ({ page }) => {
  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  await expect(page).toHaveURL(/\/admin\/pages/)

  const imageInput = page.locator('input[type="file"]').first()
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/uploads') && res.request().method() === 'POST' && res.ok()),
    imageInput.setInputFiles(path.resolve('tests/fixtures/avatar.png')),
  ])
  await expect(page.getByRole('button', { name: 'Remove Image' })).toBeVisible()

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/pages') && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Save Changes' }).nth(1).click(),
  ])

  await page.goto('/')
  const profileImage = page.locator('img[alt="Profile"]').first()
  await expect(profileImage).toBeVisible()
  await expect(profileImage).toHaveAttribute('src', /\/media\/public-assets\//)
})
