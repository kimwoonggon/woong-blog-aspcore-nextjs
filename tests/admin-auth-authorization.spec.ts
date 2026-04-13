import { expect, test, type Page } from '@playwright/test'

async function loginAsLocalUser(page: Page) {
  const baseUrl = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost'

  await page.goto(
    `${baseUrl}/api/auth/test-login?email=${encodeURIComponent('user@example.com')}&returnUrl=%2F`,
    { waitUntil: 'networkidle' },
  )

  await expect.poll(async () => {
    return await page.evaluate(async () => {
      const response = await fetch('/api/auth/session', {
        credentials: 'include',
        cache: 'no-store',
      })

      const payload = await response.json() as { authenticated?: boolean; role?: string }
      return payload.authenticated ? payload.role ?? null : null
    })
  }).toBe('user')
}

test('non-admin local sessions are redirected away from admin routes', async ({ page }) => {
  await loginAsLocalUser(page)

  await page.goto('/admin/dashboard', { waitUntil: 'networkidle' })

  await expect(page).toHaveURL(/\/(?:\?.*)?$/)
  await expect(page.getByText('Admin Panel')).toHaveCount(0)
  await expect.poll(async () => {
    return await page.evaluate(async () => {
      const response = await fetch('/api/auth/session', {
        credentials: 'include',
        cache: 'no-store',
      })

      const payload = await response.json() as { role?: string }
      return payload.role ?? null
    })
  }).toBe('user')
})
