import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can update site settings from admin pages', async ({ page }) => {
  const ownerName = `Woonggon QA ${Date.now()}`

  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  await expect(page).toHaveURL(/\/admin\/pages/)
  await page.getByRole('link', { name: 'Edit introduction page' }).click()
  await expect(page).toHaveURL(/#introduction-page-editor$/)

  await page.locator('#ownerName').fill(ownerName)
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/api/admin/site-settings') && response.request().method() === 'PUT' && response.ok()),
    page.getByRole('button', { name: 'Save Changes' }).first().click(),
  ])

  await page.goto('/')
  await expect(page.getByRole('link', { name: ownerName }).first()).toBeVisible()
  await expect(page.getByText(new RegExp(`${ownerName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}.*All rights reserved`, 'i'))).toBeVisible()
})

test('admin can update the introduction page from admin pages', async ({ page }) => {
  const introText = `Seeded introduction saved from Playwright ${Date.now()}`

  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  const introSection = page.locator('#introduction-page-editor')
  await introSection.getByLabel('Content (HTML/Text)').fill(`<p>${introText}</p>`)
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/api/admin/pages') && response.request().method() === 'PUT' && response.ok()),
    introSection.getByRole('button', { name: 'Save Changes' }).click(),
  ])

  await page.goto('/introduction')
  await expect(page.getByText(introText)).toBeVisible()
})

test('admin can update the contact page from admin pages', async ({ page }) => {
  const contactText = `Seeded contact saved from Playwright ${Date.now()}`

  page.on('dialog', (dialog) => {
    void dialog.accept().catch(() => {})
  })

  await page.goto('/admin/pages')
  const contactSection = page.locator('#contact-page-editor')
  await contactSection.getByLabel('Content (HTML/Text)').fill(`<p>${contactText}</p>`)
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/api/admin/pages') && response.request().method() === 'PUT' && response.ok()),
    contactSection.getByRole('button', { name: 'Save Changes' }).click(),
  ])

  await page.goto('/contact')
  await expect(page.locator('main')).toContainText(contactText)
  await page.screenshot({ path: 'test-results/playwright/admin-pages-settings.png', fullPage: true })
})
