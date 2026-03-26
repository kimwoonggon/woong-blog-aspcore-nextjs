import { request, type APIRequestContext, type FullConfig } from '@playwright/test'
import fs from 'node:fs/promises'
import path from 'node:path'

function shouldIgnoreHttpsErrors(baseURL: string) {
  return /^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/i.test(baseURL)
}

async function getReadyLoginPage(apiContext: APIRequestContext) {
  const response = await apiContext.get('/login')
  const body = await response.text()

  return {
    ok: response.ok() && body.includes('Continue as Local Admin'),
    status: response.status(),
  }
}

export default async function globalSetup(config: FullConfig) {
  const baseURL = config.projects[0]?.use?.baseURL as string | undefined
  if (!baseURL) {
    return
  }

  const ignoreHTTPSErrors = shouldIgnoreHttpsErrors(baseURL)
  const deadline = Date.now() + 60_000
  let lastError: unknown = null

  while (Date.now() < deadline) {
    let apiContext: APIRequestContext | null = null

    try {
      apiContext = await request.newContext({ baseURL, ignoreHTTPSErrors })
      const readiness = await getReadyLoginPage(apiContext)
      if (!readiness.ok) {
        throw new Error(`Unexpected login readiness response: ${readiness.status}`)
      }

      await apiContext.get('/api/auth/test-login?email=admin@example.com&returnUrl=%2Fadmin%2Fdashboard')
      const sessionResponse = await apiContext.get('/api/auth/session')
      const sessionPayload = await sessionResponse.json() as { authenticated?: boolean }
      if (!sessionPayload.authenticated) {
        throw new Error('Admin storage state bootstrap did not yield an authenticated session.')
      }

      const storageStatePath = path.resolve('test-results/playwright/admin-storage-state.json')
      await fs.mkdir(path.dirname(storageStatePath), { recursive: true })
      await apiContext.storageState({ path: storageStatePath })
      await apiContext.dispose()
      return
    } catch (error) {
      lastError = error
      if (apiContext) {
        await apiContext.dispose()
      }
    }

    await new Promise((resolve) => setTimeout(resolve, 1000))
  }

  throw new Error(`Playwright global setup could not reach ${baseURL}/login within 60 seconds. Last error: ${String(lastError)}`)
}
