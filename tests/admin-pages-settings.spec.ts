import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can update site settings from admin pages', async ({ page }) => {
  const ownerName = `Woonggon QA ${Date.now()}`
  const introText = `Seeded introduction saved from Playwright ${Date.now()}`

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

  await page.goto('/admin/pages')
  await page.locator('textarea').nth(1).fill(introText)
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/api/admin/pages') && response.request().method() === 'PUT' && response.ok()),
    page.getByRole('button', { name: 'Save Changes' }).nth(2).click(),
  ])

  await page.goto('/introduction')
  await expect(page.getByText(introText)).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/admin-pages-settings.png', fullPage: true })
})
