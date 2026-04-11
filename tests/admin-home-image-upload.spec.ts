import path from 'path'
import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('home page editor uploads a profile image and public home renders it', async ({ page }) => {
  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  await expect(page).toHaveURL(/\/admin\/pages/)

  const homeSection = page.locator('#home-page-editor')
  await expect(homeSection.getByText('Home Page - Hero Section')).toBeVisible()
  const imageInput = homeSection.locator('input[type="file"]')
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/uploads') && res.request().method() === 'POST' && res.ok()),
    imageInput.setInputFiles(path.resolve('tests/fixtures/avatar.png')),
  ])
  await expect(homeSection.getByRole('button', { name: 'Remove Image' })).toBeVisible()

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/pages') && res.request().method() === 'PUT' && res.ok()),
    homeSection.getByRole('button', { name: 'Save Changes' }).click(),
  ])

  await page.goto('/')
  const profileImage = page.locator('main img').first()
  await expect(profileImage).toBeVisible()
  await expect(profileImage).toHaveAttribute('src', /\/media\/public-assets\//)
})
