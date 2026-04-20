import { expect, type Page } from '@playwright/test'

async function gotoStable(page: Page, url: string) {
  for (let attempt = 0; attempt < 2; attempt += 1) {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded' })
      await expect(page.locator('body')).toBeVisible({ timeout: 15000 })
      return
    } catch (error) {
      if (!(error instanceof Error) || !error.message.includes('ERR_ABORTED') || attempt === 1) {
        throw error
      }
    }
  }
}

export async function loginAsLocalAdmin(page: Page, returnPath = '/') {
  const baseUrl = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost'

  await gotoStable(
    page,
    `${baseUrl}/api/auth/test-login?email=admin%40example.com&returnUrl=%2Fadmin%2Fdashboard`,
  )
  await expect(page).toHaveURL(/\/admin(?:\/dashboard)?$/, { timeout: 15000 })
  await expect(page.getByRole('heading', { name: /Dashboard|Admin Panel/ }).first()).toBeVisible({ timeout: 15000 })
  if (new URL(page.url()).pathname === returnPath) {
    return
  }
  await gotoStable(page, `${baseUrl}${returnPath}`)
}
