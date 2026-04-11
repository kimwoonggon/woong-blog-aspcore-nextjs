import { expect, test } from '@playwright/test'
import { gotoWithTheme } from './helpers/ui-improvement'

test('admin layout uses semantic color tokens in dark mode', async ({ page }) => {
  await gotoWithTheme(page, '/admin/dashboard')

  const aside = page.locator('aside').first()
  const main = page.locator('main').first()
  await expect(aside).toBeVisible()
  await expect(aside).toHaveClass(/bg-background/)
  await expect(main).toHaveClass(/bg-muted\/30/)
})
