import { expect, type Page } from '@playwright/test'

export async function loginAsLocalAdmin(page: Page, returnPath = '/') {
  const baseUrl = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost'

  await page.goto(`${baseUrl}/login`, { waitUntil: 'networkidle' })
  await page.getByRole('button', { name: 'Continue as Local Admin' }).click()
  await expect(page).toHaveURL(/\/admin(?:\/dashboard)?$/, { timeout: 15000 })
  await expect.poll(async () => {
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
  }, { timeout: 15000 }).toBe(true)
  await page.goto(`${baseUrl}${returnPath}`, { waitUntil: 'networkidle' })
}
