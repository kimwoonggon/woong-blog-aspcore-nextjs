import { expect, test } from '@playwright/test'
import { getStyle } from './helpers/ui-improvement'

test('VA-033 layering keeps navbar above dropdowns and modal layers above dropdown content', async ({ page }) => {
  await page.goto('/')

  const navbar = page.locator('header').first()
  await expect(navbar).toBeVisible()
  const navbarZ = Number.parseInt(await getStyle(navbar, 'z-index'), 10)

  await page.getByTestId('theme-toggle').click()
  const dropdown = page.locator('[data-slot="dropdown-menu-content"]').first()
  await expect(dropdown).toBeVisible()
  const dropdownZ = Number.parseInt(await getStyle(dropdown, 'z-index'), 10)

  expect(navbarZ).toBeGreaterThan(dropdownZ)
})

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('VA-033 dialogs render above dropdown layering', async ({ page }) => {
  await page.goto('/admin/blog')
  await page.getByRole('button', { name: 'Delete' }).first().click()

  const dialog = page.locator('[data-slot="dialog-content"]').first()
  await expect(dialog).toBeVisible()
  const dialogZ = Number.parseInt(await getStyle(dialog, 'z-index'), 10)
  expect(dialogZ).toBeGreaterThan(50)
})
