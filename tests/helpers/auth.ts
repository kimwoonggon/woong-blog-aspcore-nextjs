import { expect, type Page } from '@playwright/test'

async function gotoStable(page: Page, url: string) {
  for (let attempt = 0; attempt < 2; attempt += 1) {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded' })
      await page.waitForLoadState('networkidle')
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

  await gotoStable(page, `${baseUrl}/login`)
  await page.getByRole('button', { name: 'Continue as Local Admin' }).click()
  await expect(page).toHaveURL(/\/admin(?:\/dashboard)?$/, { timeout: 15000 })
  await expect.poll(async () => {
    try {
      return await page.evaluate(async () => {
        try {
          const response = await fetch('/api/auth/session', {
            credentials: 'include',
            cache: 'no-store',
          })
          if (!response.ok) return false
          const payload = await response.json() as { authenticated?: boolean }
          return payload.authenticated === true
        } catch {
          return false
        }
      })
    } catch {
      return false
    }
  }, { timeout: 15000 }).toBe(true)
  await gotoStable(page, `${baseUrl}${returnPath}`)
}
