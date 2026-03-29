import { expect, test } from '@playwright/test'
import { loginAsLocalAdmin } from './helpers/auth'

test('managed test server exposes proxied auth session and local admin bootstrap', async ({ page, request }) => {
  await page.goto('/login', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: 'Continue as Local Admin' })).toBeVisible()

  const anonymousSession = await request.get('/api/auth/session')
  expect(anonymousSession.ok()).toBeTruthy()
  await expect(anonymousSession.json()).resolves.toEqual({ authenticated: false })

  await loginAsLocalAdmin(page, '/admin/dashboard')
  await expect(page).toHaveURL(/\/admin\/dashboard$/)

  const authenticatedSession = await page.evaluate(async () => {
    const response = await fetch('/api/auth/session', {
      credentials: 'include',
      cache: 'no-store',
    })

    return response.json() as Promise<{ authenticated?: boolean; role?: string }>
  })

  expect(authenticatedSession.authenticated).toBe(true)
  expect(authenticatedSession.role).toBe('admin')
})
