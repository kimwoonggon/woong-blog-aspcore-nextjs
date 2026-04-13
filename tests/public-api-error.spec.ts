import { expect, test } from '@playwright/test'

test('CF-005 shows a user-facing fallback when a public page payload is malformed', async ({ page }) => {
  await page.goto('/introduction?__qaBroken=1')

  await expect(page.getByText('Public pages')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'This page could not be loaded.' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible()
})
